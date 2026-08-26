using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Files;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseWriter(
    IDbSession dbSession,
    ICaseNumberIssuer numbers,
    IFileBlobStore fileBlobStore,
    ILogger<CaseWriter> logger) : ICaseWriter
{
    /// <summary>
    /// How many numbers one case may be issued. The generator reads the day's highest and the unique index
    /// settles the race, so the loser of one files again with the number the winner left free (SDD-008).
    /// </summary>
    private const int Attempts = 5;

    public async Task<CaseCreateResult> CreateCase(CreateCaseRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        if (request.ParentCaseId is { } parentCaseId)
        {
            var known = await context.Cases
                .WithId(parentCaseId)
                .AnyAsync(token);

            if (!known)
                return new CaseCreateResult { Outcome = CaseCreateOutcome.InvalidParent };
        }

        for (var attempt = 1; ; attempt++)
        {
            var caseNumber = await numbers.NextCaseNumber(request.Date, token);
            var @case = BuildCase(request, caseNumber);

            context.Cases.Add(@case);

            try
            {
                await context.SaveChangesAsync(token);
            }
            catch (DbUpdateException exception) when (attempt < Attempts && exception.IsUniqueViolation())
            {
                context.Entry(@case).State = EntityState.Detached;

                logger.LogWarning("The case number {CaseNumber} was taken while the case was being filed", caseNumber);

                continue;
            }

            logger.LogInformation("Case {CaseId} was filed under {CaseNumber}", @case.Id, @case.CaseNumber);

            return new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = Describe(@case) };
        }
    }

    /// <summary>
    /// <c>TenantId</c> and <c>UserId</c> are left unset here, the way the sample seeder leaves them
    /// (SDD-018): the write stamps both from <c>IUserContext</c>.
    /// </summary>
    internal static Case BuildCase(CreateCaseRequest request, string caseNumber)
    {
        return new()
        {
            ParentCaseId = request.ParentCaseId,
            CaseNumber = caseNumber,
            Title = request.Title,
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description,
            Date = request.Date,
            Status = CaseStatus.Active,
        };
    }

    public async Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token)
    {
        var edit = Normalize(request);

        if (CaseNumberFormat.ParseOrDefault(edit.CaseNumber) is null)
            return CaseUpdateOutcome.InvalidCaseNumber;

        var context = dbSession.Current;

        var taken = await context.Cases
            .WithNumberHeldByAnother(edit.CaseNumber, caseId)
            .AnyAsync(token);

        if (taken)
            return CaseUpdateOutcome.CaseNumberTaken;

        if (edit.ParentCaseId is { } parentCaseId)
        {
            var parents = await context.Cases
                .Select(@case => new { @case.Id, @case.ParentCaseId })
                .ToDictionaryAsync(link => link.Id, link => link.ParentCaseId, token);

            if (!parents.ContainsKey(parentCaseId) || CaseHierarchy.WouldFormCycle(parents, caseId, parentCaseId))
                return CaseUpdateOutcome.InvalidParent;
        }

        var rows = await context.Cases
            .WithId(caseId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(@case => @case.CaseNumber, edit.CaseNumber)
                    .SetProperty(@case => @case.ParentCaseId, edit.ParentCaseId)
                    .SetProperty(@case => @case.Date, edit.Date)
                    .SetProperty(@case => @case.Title, edit.Title)
                    .SetProperty(@case => @case.Description, edit.Description)
                    .SetProperty(@case => @case.Status, edit.Status),
                token);

        if (rows == 0)
            return CaseUpdateOutcome.NotFound;

        logger.LogInformation("Case {CaseId} was edited", caseId);

        return CaseUpdateOutcome.Updated;
    }

    internal static CaseEditRequest Normalize(CaseEditRequest request)
    {
        return request with
        {
            CaseNumber = request.CaseNumber.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description?.TrimEmptyToNull(),
        };
    }

    public async Task<bool> DeleteCase(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        // Read before the delete: the rows are gone once it runs.
        var storagePaths = await context.FileAssets
            .Where(file => file.CaseId == caseId || file.Act!.CaseId == caseId)
            .Select(file => file.StoragePath)
            .ToListAsync(token);

        // The acts, comments, marks and files go with the row: the database's foreign keys carry the
        // cascade, and a subordinate case is left with no parent (SDD-007).
        var rows = await context.Cases
            .WithId(caseId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return false;

        // After the commit: a blob orphaned by a failed delete is tolerated, a lost one is not (SDD-012).
        foreach (var storagePath in storagePaths)
            await fileBlobStore.DeleteFileBlob(storagePath, token);

        logger.LogInformation("Case {CaseId} was deleted", caseId);

        return true;
    }

    private static CaseListItem Describe(Case @case)
    {
        return new()
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
        };
    }
}

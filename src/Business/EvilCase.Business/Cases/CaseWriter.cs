using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseWriter(
    IDbSession dbSession,
    ICaseNumberIssuer numbers,
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
            var known = await context.Cases.Exists(parentCaseId, token);

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
            Title = request.Title.Trim(),
            Description = request.Description?.TrimEmptyToNull(),
            Date = request.Date,
            Status = CaseStatus.Active,
        };
    }

    public async Task<CaseUpdateOutcome> UpdateCase(Guid caseId, CaseEditRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        // A case the tenant does not have is not found, whatever else the edit gets wrong (R-025).
        if (!await context.Cases.WithId(caseId).AnyAsync(token))
            return CaseUpdateOutcome.NotFound;

        var edit = request with
        {
            CaseNumber = request.CaseNumber.Trim(),
            Title = request.Title.Trim(),
            Description = request.Description?.TrimEmptyToNull(),
        };

        if (CaseNumberFormat.ParseOrDefault(edit.CaseNumber) is null)
            return CaseUpdateOutcome.InvalidCaseNumber;

        var taken = await context.Cases
            .WithNumberHeldByAnother(edit.CaseNumber, caseId)
            .AnyAsync(token);

        if (taken)
            return CaseUpdateOutcome.CaseNumberTaken;

        if (edit.ParentCaseId is { } parentCaseId)
        {
            var parents = await context.Cases
                .Select(static @case => new { @case.Id, @case.ParentCaseId })
                .ToDictionaryAsync(static link => link.Id, static link => link.ParentCaseId, token);

            if (!parents.ContainsKey(parentCaseId) || CaseHierarchy.WouldFormCycle(parents, caseId, parentCaseId))
                return CaseUpdateOutcome.InvalidParent;
        }

        var rows = await context.Cases
            .WithId(caseId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(static @case => @case.CaseNumber, edit.CaseNumber)
                    .SetProperty(static @case => @case.ParentCaseId, edit.ParentCaseId)
                    .SetProperty(static @case => @case.Date, edit.Date)
                    .SetProperty(static @case => @case.Title, edit.Title)
                    .SetProperty(static @case => @case.Description, edit.Description)
                    .SetProperty(static @case => @case.Status, edit.Status),
                token);

        if (rows == 0)
            return CaseUpdateOutcome.NotFound;

        logger.LogInformation("Case {CaseId} was edited", caseId);

        return CaseUpdateOutcome.Updated;
    }

    public async Task<DeleteOutcome> DeleteCase(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        // One transaction, so now() stamps the case and everything under it with one moment.
        await using var transaction = await dbSession.BeginTransaction(token);

        var caseIds = await CaseWithItsSubordinates(context.Cases, caseId, token);
        if (caseIds.Count == 0)
            return DeleteOutcome.NotFound;

        var actIds = await context.Acts
            .Where(act => caseIds.Contains(act.CaseId))
            .Select(static act => act.Id)
            .ToListAsync(token);

        // The act ids are read up front, so stamping the acts cannot hide the rows hanging off them.
        await context.Comments
            .Where(comment => caseIds.Contains(comment.CaseId!.Value) || actIds.Contains(comment.ActId!.Value))
            .ExecuteSoftDelete(token);

        await context.FileAssets
            .Where(file => caseIds.Contains(file.CaseId!.Value) || actIds.Contains(file.ActId!.Value))
            .ExecuteSoftDelete(token);

        await context.ExternalActNumbers
            .Where(number => actIds.Contains(number.ActId))
            .ExecuteSoftDelete(token);

        await context.ExternalCaseNumbers
            .Where(number => caseIds.Contains(number.CaseId))
            .ExecuteSoftDelete(token);

        await context.Acts
            .Where(act => caseIds.Contains(act.CaseId))
            .ExecuteSoftDelete(token);

        await context.Cases
            .Where(@case => caseIds.Contains(@case.Id))
            .ExecuteSoftDelete(token);

        await transaction.CommitAsync(token);

        // The blobs stay: a stamped file is a file that can come back (SDD-012).
        logger.LogInformation("Case {CaseId} was deleted", caseId);

        return DeleteOutcome.Deleted;
    }

    /// <summary>
    /// The case and every case under it. A parent is optional and a cycle is refused (SDD-009), so
    /// the descent ends.
    /// </summary>
    private static async Task<List<Guid>> CaseWithItsSubordinates(IQueryable<Case> cases, Guid caseId, CancellationToken token)
    {
        var caseIds = await cases
            .WithId(caseId)
            .Select(static @case => @case.Id)
            .ToListAsync(token);

        for (var parentIds = caseIds; parentIds.Count != 0;)
        {
            parentIds = await cases
                .Where(@case => parentIds.Contains(@case.ParentCaseId!.Value))
                .Select(static @case => @case.Id)
                .ToListAsync(token);

            caseIds.AddRange(parentIds);
        }

        return caseIds;
    }

    private static CaseListItem Describe(Case @case)
    {
        return new()
        {
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
            Changed = @case.Updated ?? @case.Created,
        };
    }
}

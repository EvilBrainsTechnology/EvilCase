using EvilBrains.Collections;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
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
    /// The unique index settles the numbering race; a loser retries (SDD-008).
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

        if (request.ContactId is { } contactId && !await context.Contacts.Exists(contactId, token))
            return new CaseCreateResult { Outcome = CaseCreateOutcome.ContactNotFound };

        if (await context.Labels.ReadLabels(request.LabelIds, token) is not { } labels)
            return new CaseCreateResult { Outcome = CaseCreateOutcome.LabelNotFound };

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

            if (labels.Count != 0)
            {
                LabelAssignmentWrites.AddCaseLabels(context, @case.Id, labels);
                await context.SaveChangesAsync(token);
            }

            logger.LogInformation("Case {CaseId} was filed under {CaseNumber}", @case.Id, @case.CaseNumber);

            return new CaseCreateResult { Outcome = CaseCreateOutcome.Created, Case = Describe(@case, labels) };
        }
    }

    // TenantId and UserId are stamped by UserWriteInterceptor.
    internal static Case BuildCase(CreateCaseRequest request, string caseNumber)
    {
        return new()
        {
            ParentCaseId = request.ParentCaseId,
            ContactId = request.ContactId,
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
            ExternalCaseNumber = request.ExternalCaseNumber?.TrimEmptyToNull(),
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

        if (edit.ParentCaseId is { } parentCaseId && !await this.ParentAllowed(caseId, parentCaseId, token))
            return CaseUpdateOutcome.InvalidParent;

        if (edit.ContactId is { } contactId && !await context.Contacts.Exists(contactId, token))
            return CaseUpdateOutcome.ContactNotFound;

        if (await context.Labels.ReadLabels(edit.LabelIds, token) is not { } labels)
            return CaseUpdateOutcome.LabelNotFound;

        var rows = await context.Cases
            .WithId(caseId)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(static @case => @case.CaseNumber, edit.CaseNumber)
                    .SetProperty(static @case => @case.ExternalCaseNumber, edit.ExternalCaseNumber)
                    .SetProperty(static @case => @case.ParentCaseId, edit.ParentCaseId)
                    .SetProperty(static @case => @case.ContactId, edit.ContactId)
                    .SetProperty(static @case => @case.Date, edit.Date)
                    .SetProperty(static @case => @case.Title, edit.Title)
                    .SetProperty(static @case => @case.Description, edit.Description)
                    .SetProperty(static @case => @case.Status, edit.Status),
                token);

        if (rows == 0)
            return CaseUpdateOutcome.NotFound;

        await LabelAssignmentWrites.ReplaceCaseLabels(context, caseId, labels, token);

        logger.LogInformation("Case {CaseId} was edited", caseId);

        return CaseUpdateOutcome.Updated;
    }

    private static CaseListItem Describe(Case @case, IReadOnlyList<LabelItem> labels)
    {
        return new()
        {
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            Title = @case.Title,
            Date = @case.Date,
            Status = @case.Status,
            Changed = @case.Updated ?? @case.Created,
            Labels = labels,
        };
    }

    /// <summary>
    /// The parent must exist and must close no cycle (SDD-009).
    /// </summary>
    private async Task<bool> ParentAllowed(Guid caseId, Guid parentCaseId, CancellationToken token)
    {
        var parents = await dbSession.Current.Cases
            .Select(static @case => new { @case.Id, @case.ParentCaseId })
            .ToDictionaryAsync(static link => link.Id, static link => link.ParentCaseId, token);

        return parents.ContainsKey(parentCaseId) && !CaseHierarchy.WouldFormCycle(parents, caseId, parentCaseId);
    }

    public async Task<DeleteOutcome> DeleteCase(Guid caseId, CancellationToken token)
    {
        var context = dbSession.Current;

        // The subordinate cases, acts, comments and files go with the row: the database's foreign keys
        // carry the cascade down the whole subtree (SDD-007). Their blobs stay on disk (SDD-012).
        var rows = await context.Cases
            .WithId(caseId)
            .ExecuteDeleteAsync(token);

        if (rows == 0)
            return DeleteOutcome.NotFound;

        logger.LogInformation("Case {CaseId} was deleted", caseId);

        return DeleteOutcome.Deleted;
    }
}

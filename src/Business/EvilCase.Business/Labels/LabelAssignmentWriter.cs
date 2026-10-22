using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Labels;

internal sealed class LabelAssignmentWriter(IDbSession dbSession, ILogger<LabelAssignmentWriter> logger) : ILabelAssignmentWriter
{
    public async Task<LabelAssignmentOutcome> SetCaseLabels(Guid caseId, LabelAssignmentRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        if (!await context.Cases.Exists(caseId, token))
            return LabelAssignmentOutcome.OwnerNotFound;

        if (await context.Labels.ReadLabels(request.LabelIds, token) is not { } labels)
            return LabelAssignmentOutcome.LabelNotFound;

        await using var transaction = await dbSession.BeginTransaction(token);

        await LabelAssignmentWrites.ReplaceCaseLabels(context, caseId, labels, token);

        await transaction.CommitAsync(token);

        logger.LogInformation("Labels of case {CaseId} were set", caseId);

        return LabelAssignmentOutcome.Assigned;
    }

    public async Task<LabelAssignmentOutcome> SetActLabels(Guid actId, LabelAssignmentRequest request, CancellationToken token)
    {
        var context = dbSession.Current;

        if (!await context.Acts.WithId(actId).AnyAsync(token))
            return LabelAssignmentOutcome.OwnerNotFound;

        if (await context.Labels.ReadLabels(request.LabelIds, token) is not { } labels)
            return LabelAssignmentOutcome.LabelNotFound;

        await using var transaction = await dbSession.BeginTransaction(token);

        await LabelAssignmentWrites.ReplaceActLabels(context, actId, labels, token);

        await transaction.CommitAsync(token);

        logger.LogInformation("Labels of act {ActId} were set", actId);

        return LabelAssignmentOutcome.Assigned;
    }
}

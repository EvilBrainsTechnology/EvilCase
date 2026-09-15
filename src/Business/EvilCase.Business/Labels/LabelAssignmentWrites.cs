using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Labels;

/// <summary>
/// Both ends take labels already read, so the caller has refused an unknown id before it writes.
/// </summary>
internal static class LabelAssignmentWrites
{
    /// <summary>
    /// A fresh row carries no assignment, so its labels are only added; the caller saves them.
    /// </summary>
    public static void AddCaseLabels(ApplicationDbContext context, Guid caseId, IReadOnlyList<LabelItem> labels)
    {
        Add(context, labels, caseId, actId: null);
    }

    public static void AddActLabels(ApplicationDbContext context, Guid actId, IReadOnlyList<LabelItem> labels)
    {
        Add(context, labels, caseId: null, actId);
    }

    public static async Task ReplaceCaseLabels(ApplicationDbContext context, Guid caseId, IReadOnlyList<LabelItem> labels, CancellationToken token)
    {
        await Replace(context, context.LabelAssignments.OfCase(caseId), labels, caseId, actId: null, token);
    }

    public static async Task ReplaceActLabels(ApplicationDbContext context, Guid actId, IReadOnlyList<LabelItem> labels, CancellationToken token)
    {
        await Replace(context, context.LabelAssignments.OfAct(actId), labels, caseId: null, actId, token);
    }

    private static void Add(ApplicationDbContext context, IReadOnlyList<LabelItem> labels, Guid? caseId, Guid? actId)
    {
        foreach (var label in labels)
            context.LabelAssignments.Add(new LabelAssignment { LabelId = label.LabelId, CaseId = caseId, ActId = actId });
    }

    private static async Task Replace(
        ApplicationDbContext context,
        IQueryable<LabelAssignment> held,
        IReadOnlyList<LabelItem> labels,
        Guid? caseId,
        Guid? actId,
        CancellationToken token)
    {
        var wanted = labels.Select(static label => label.LabelId).ToList();

        await held
            .Where(assignment => !wanted.Contains(assignment.LabelId))
            .ExecuteDeleteAsync(token);

        var carried = await held
            .Select(static assignment => assignment.LabelId)
            .ToListAsync(token);

        foreach (var labelId in wanted.Where(labelId => !carried.Contains(labelId)))
            context.LabelAssignments.Add(new LabelAssignment { LabelId = labelId, CaseId = caseId, ActId = actId });

        await context.SaveChangesAsync(token);
    }
}

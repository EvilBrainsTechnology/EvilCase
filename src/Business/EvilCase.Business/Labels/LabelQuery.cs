using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Labels;

internal static class LabelQuery
{
    public static IQueryable<Label> InListOrder(this IQueryable<Label> labels)
    {
        return labels
            .OrderBy(static label => label.Name)
            .ThenBy(static label => label.Id);
    }

    public static IQueryable<Label> WithNameHeldByAnother(this IQueryable<Label> labels, string name, Guid labelId)
    {
        return labels
            .Where(label => label.Name == name)
            .Where(label => label.Id != labelId);
    }

    public static IQueryable<LabelItem> AsListItems(this IQueryable<Label> labels)
    {
        return labels.Select(static label => new LabelItem
        {
            LabelId = label.Id,
            Name = label.Name,
            Color = label.Color,
        });
    }

    /// <summary>
    /// Null where an id is unknown to the tenant; the foreign key alone would take another tenant's
    /// label, so this filtered read is what refuses it.
    /// </summary>
    public static async Task<IReadOnlyList<LabelItem>?> ReadLabels(this IQueryable<Label> labels, IReadOnlyList<Guid> labelIds, CancellationToken token)
    {
        var wanted = labelIds.Distinct().ToList();

        if (wanted.Count == 0)
            return [];

        var items = await labels
            .Where(label => wanted.Contains(label.Id))
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);

        return items.Count == wanted.Count ? items : null;
    }

    public static IQueryable<LabelAssignment> OfCase(this IQueryable<LabelAssignment> assignments, Guid caseId)
    {
        return assignments.Where(assignment => assignment.CaseId == caseId);
    }

    public static IQueryable<LabelAssignment> OfAct(this IQueryable<LabelAssignment> assignments, Guid actId)
    {
        return assignments.Where(assignment => assignment.ActId == actId);
    }
}

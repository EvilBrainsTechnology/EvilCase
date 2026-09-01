using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Shapes an act list, one composable step per rule.
/// </summary>
internal static class ActListQuery
{
    /// <summary>
    /// Oldest first by the act's own date; equal dates fall back to when the row was written, and the
    /// UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<Act> InListOrder(this IQueryable<Act> acts)
    {
        return acts
            .OrderBy(act => act.Date)
            .ThenBy(act => act.Created)
            .ThenBy(act => act.Id);
    }

    public static IQueryable<Act> OfCase(this IQueryable<Act> acts, Guid caseId)
    {
        return acts.Where(act => act.CaseId == caseId);
    }

    /// <summary>
    /// Newest by the act's own date; equal dates fall back to when the row was written, and the
    /// UUIDv7 identifier makes the order total.
    /// </summary>
    public static IQueryable<Act> InLatestOrder(this IQueryable<Act> acts)
    {
        return acts
            .OrderByDescending(act => act.Date)
            .ThenByDescending(act => act.Created)
            .ThenByDescending(act => act.Id);
    }

    /// <summary>
    /// Reads only what a row shows, in one query.
    /// </summary>
    public static IQueryable<ActListItem> AsListItems(this IQueryable<Act> acts)
    {
        return acts.Select(act => new ActListItem
        {
            ActId = act.Id,
            CaseId = act.CaseId,
            CaseNumber = act.Case!.CaseNumber,
            ActNumber = act.ActNumber,
            Direction = act.Direction,
            Title = act.Title,
            Date = act.Date,
            IssuedByName = act.IssuedByContact!.Name,
            AddressedToName = act.AddressedToContact!.Name,
        });
    }
}

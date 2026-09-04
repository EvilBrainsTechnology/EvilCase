using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

internal static class ActListQuery
{
    public static IQueryable<Act> InListOrder(this IQueryable<Act> acts)
    {
        return acts
            .OrderBy(static act => act.Date)
            .ThenBy(static act => act.Created)
            .ThenBy(static act => act.Id);
    }

    public static IQueryable<Act> OfCase(this IQueryable<Act> acts, Guid caseId)
    {
        return acts.Where(act => act.CaseId == caseId);
    }

    public static IQueryable<Act> InLatestOrder(this IQueryable<Act> acts)
    {
        return acts
            .OrderByDescending(static act => act.Date)
            .ThenByDescending(static act => act.Created)
            .ThenByDescending(static act => act.Id);
    }

    public static IQueryable<ActListItem> AsListItems(this IQueryable<Act> acts)
    {
        return acts.Select(static act => new ActListItem
        {
            ActId = act.Id,
            CaseId = act.CaseId,
            CaseNumber = act.Case!.CaseNumber,
            ActNumber = act.ActNumber,
            Direction = act.Direction,
            Title = act.Title,
            Date = act.Date,
            ContactName = act.Contact!.Name,
        });
    }
}

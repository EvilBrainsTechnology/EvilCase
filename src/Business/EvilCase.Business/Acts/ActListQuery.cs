using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>
/// Shapes an act list, one composable step per rule.
/// </summary>
public static class ActListQuery
{
    /// <summary>
    /// Oldest first, by the act date alone, the identifier breaking the tie so the order is total.
    /// </summary>
    public static IQueryable<Act> InListOrder(this IQueryable<Act> acts)
    {
        return acts
            .OrderBy(act => act.Date)
            .ThenBy(act => act.Id);
    }
}

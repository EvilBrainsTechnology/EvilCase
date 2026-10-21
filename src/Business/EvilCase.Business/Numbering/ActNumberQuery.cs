using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal static class ActNumberQuery
{
    /// <summary>
    /// Every act of the tenant under the prefix counts, whichever case holds it now.
    /// A hand-edited number outside the format drops out here.
    /// </summary>
    public static IQueryable<Act> WithNumberPrefix(this IQueryable<Act> acts, string prefix)
    {
        var pattern = prefix.EscapeLikeWildcards() + "%";

        return acts
            .Where(act => EF.Functions.Like(act.ActNumber, pattern, LikeExtensions.LikeEscape));
    }

    /// <summary>
    /// Length first: under string order 1000 sorts below 999.
    /// </summary>
    public static IQueryable<Act> OrderByNumberDescending(this IQueryable<Act> acts)
    {
        return acts
            .OrderByDescending(static act => act.ActNumber.Length)
            .ThenByDescending(static act => act.ActNumber);
    }

    public static IQueryable<Act> WithNumberHeldByAnother(this IQueryable<Act> acts, string actNumber, Guid actId)
    {
        return acts
            .Where(act => act.ActNumber == actNumber)
            .Where(act => act.Id != actId);
    }
}

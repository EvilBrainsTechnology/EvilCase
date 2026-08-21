using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Reads what the next act number of a day inside a case needs, one composable step per rule.
/// </summary>
public static class ActNumberQuery
{
    /// <summary>
    /// The case's own numbers of the day. A hand-written case number carries another prefix and drops out here.
    /// </summary>
    public static IQueryable<Act> OfCaseWithNumberPrefix(this IQueryable<Act> acts, Guid caseId, string prefix)
    {
        ArgumentNullException.ThrowIfNull(acts);
        ArgumentNullException.ThrowIfNull(prefix);

        var pattern = prefix.EscapeLikeWildcards() + "%";

        return acts.Where(act => act.CaseId == caseId && EF.Functions.Like(act.ActNumber, pattern, LikeExtensions.LikeEscape));
    }

    /// <summary>
    /// The highest number of the narrowed set, as one row. Length decides first, so a sequence that grew a
    /// digit outranks a three-digit one.
    /// </summary>
    public static IQueryable<string> HighestNumber(this IQueryable<Act> acts)
    {
        ArgumentNullException.ThrowIfNull(acts);

        return acts
            .Select(act => act.ActNumber)
            .OrderByDescending(number => number.Length)
            .ThenByDescending(number => number)
            .Take(1);
    }
}

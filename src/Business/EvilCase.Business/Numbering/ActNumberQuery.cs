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
        var pattern = prefix.EscapeLikeWildcards() + "%";

        return acts
            .Where(act => act.CaseId == caseId)
            .Where(act => EF.Functions.Like(act.ActNumber, pattern, LikeExtensions.LikeEscape));
    }

    /// <summary>
    /// Highest number first. Length decides first, so a sequence that grew a digit outranks a
    /// three-digit one. The caller takes the row it wants.
    /// </summary>
    public static IQueryable<Act> OrderByNumberDescending(this IQueryable<Act> acts)
    {
        return acts
            .OrderByDescending(act => act.ActNumber.Length)
            .ThenByDescending(act => act.ActNumber);
    }
}

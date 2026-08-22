using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// Reads what the next case number of a day needs, one composable step per rule.
/// </summary>
public static class CaseNumberQuery
{
    /// <summary>
    /// The day's own numbers. A hand-written value outside the format carries another prefix and drops out here.
    /// </summary>
    public static IQueryable<Case> WithNumberPrefix(this IQueryable<Case> cases, string prefix)
    {
        var pattern = prefix.EscapeLikeWildcards() + "%";

        return cases.Where(@case => EF.Functions.Like(@case.CaseNumber, pattern, LikeExtensions.LikeEscape));
    }

    /// <summary>
    /// Highest number first. Length decides first, so a sequence that grew a digit outranks a
    /// three-digit one. The caller takes the row it wants.
    /// </summary>
    public static IQueryable<Case> OrderByNumberDescending(this IQueryable<Case> cases) => cases
        .OrderByDescending(@case => @case.CaseNumber.Length)
        .ThenByDescending(@case => @case.CaseNumber);
}

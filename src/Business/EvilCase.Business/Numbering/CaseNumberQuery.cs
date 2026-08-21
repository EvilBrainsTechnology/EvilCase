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
        ArgumentNullException.ThrowIfNull(cases);
        ArgumentNullException.ThrowIfNull(prefix);

        var pattern = prefix.EscapeLikeWildcards() + "%";

        return cases.Where(@case => EF.Functions.Like(@case.CaseNumber, pattern, LikeExtensions.LikeEscape));
    }

    /// <summary>
    /// The highest number of the narrowed set, as one row. Length decides first, so a sequence that grew a
    /// digit outranks a three-digit one.
    /// </summary>
    public static IQueryable<string> HighestNumber(this IQueryable<Case> cases)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases
            .Select(@case => @case.CaseNumber)
            .OrderByDescending(number => number.Length)
            .ThenByDescending(number => number)
            .Take(1);
    }
}

using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

internal static class CaseNumberQuery
{
    /// <summary>
    /// A hand-edited number outside the format drops out here.
    /// </summary>
    public static IQueryable<Case> WithNumberPrefix(this IQueryable<Case> cases, string prefix)
    {
        var pattern = prefix.EscapeLikeWildcards() + "%";

        return cases.Where(@case => EF.Functions.Like(@case.CaseNumber, pattern, LikeExtensions.LikeEscape));
    }

    /// <summary>
    /// Length first: under string order 1000 sorts below 999.
    /// </summary>
    public static IQueryable<Case> OrderByNumberDescending(this IQueryable<Case> cases)
    {
        return cases
            .OrderByDescending(static @case => @case.CaseNumber.Length)
            .ThenByDescending(static @case => @case.CaseNumber);
    }

    public static IQueryable<Case> WithNumberHeldByAnother(this IQueryable<Case> cases, string caseNumber, Guid caseId)
    {
        return cases
            .Where(@case => @case.CaseNumber == caseNumber)
            .Where(@case => @case.Id != caseId);
    }
}

using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

/// <summary>
/// The reads a number issuer needs, one composable step per rule.
/// </summary>
public static class NumberQuery
{
    /// <summary>
    /// The case numbers of one day, inside the tenant the query filter already scopes to.
    /// </summary>
    public static IQueryable<string> CaseNumbersOfDay(this IQueryable<Case> cases, in DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var pattern = CaseNumberFormat.DayPrefix(date) + "%";

        return cases.Where(@case => EF.Functions.Like(@case.CaseNumber, pattern)).Select(@case => @case.CaseNumber);
    }

    // A case holds few acts, and the day is counted in memory so numbers issued under a rewritten case number still count.
    public static IQueryable<string> ActNumbersOfCase(this IQueryable<Act> acts, Guid caseId)
    {
        ArgumentNullException.ThrowIfNull(acts);

        return acts.Where(act => act.CaseId == caseId).Select(act => act.ActNumber);
    }

    public static IQueryable<Case> WithCaseNumber(this IQueryable<Case> cases, string number, Guid? excluding)
    {
        ArgumentNullException.ThrowIfNull(cases);

        var found = cases.Where(@case => @case.CaseNumber == number);

        return excluding is { } id ? found.Where(@case => @case.Id != id) : found;
    }

    public static IQueryable<Act> WithActNumber(this IQueryable<Act> acts, string number, Guid? excluding)
    {
        ArgumentNullException.ThrowIfNull(acts);

        var found = acts.Where(act => act.ActNumber == number);

        return excluding is { } id ? found.Where(act => act.Id != id) : found;
    }
}

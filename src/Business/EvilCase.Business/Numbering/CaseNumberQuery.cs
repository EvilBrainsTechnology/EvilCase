using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Numbering;

public static class CaseNumberQuery
{
    /// <summary>
    /// The cases whose own number belongs to the day, whatever date the case itself carries.
    /// </summary>
    public static IQueryable<Case> WithNumberOfDay(this IQueryable<Case> cases, in DateOnly date)
    {
        ArgumentNullException.ThrowIfNull(cases);

        // The prefix is composed and never typed, so it carries no wildcard to escape.
        var pattern = CaseNumberFormat.DayPrefix(date) + "%";

        return cases.Where(@case => EF.Functions.Like(@case.CaseNumber, pattern));
    }
}

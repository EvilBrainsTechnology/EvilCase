using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

public static class CaseNumberQuery
{
    /// <summary>
    /// The cases already carrying the number, the one being edited aside.
    /// </summary>
    public static IQueryable<Case> WithNumberTakenFrom(this IQueryable<Case> cases, string caseNumber, Guid exceptId)
    {
        ArgumentNullException.ThrowIfNull(cases);

        return cases.Where(@case => @case.CaseNumber == caseNumber && @case.Id != exceptId);
    }
}

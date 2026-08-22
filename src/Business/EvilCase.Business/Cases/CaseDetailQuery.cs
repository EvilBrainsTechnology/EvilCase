using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Cases;

public static class CaseDetailQuery
{
    /// <summary>
    /// Reads only what the detail shows, in one query.
    /// </summary>
    public static IQueryable<CaseDetail> AsDetails(this IQueryable<Case> cases)
    {
        return cases.Select(@case => new CaseDetail
        {
            Id = @case.Id,
            CaseNumber = @case.CaseNumber,
            Date = @case.Date,
            Title = @case.Title,
            Description = @case.Description,
            Status = @case.Status,
        });
    }
}

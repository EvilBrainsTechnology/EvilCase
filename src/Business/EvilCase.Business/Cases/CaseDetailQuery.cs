using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// Reads the header of one case.
/// </summary>
internal static class CaseDetailQuery
{
    /// <summary>
    /// The one case, or null where the tenant has no such case.
    /// </summary>
    public static Task<CaseDetail?> DetailOf(this IQueryable<Case> cases, Guid caseId, CancellationToken token)
    {
        return cases
            .WithId(caseId)
            .Select(@case => new CaseDetail
            {
                Id = @case.Id,
                CaseNumber = @case.CaseNumber,
                Date = @case.Date,
                Title = @case.Title,
                Description = @case.Description,
                Status = @case.Status,
            })
            .SingleOrDefaultAsync(token);
    }
}

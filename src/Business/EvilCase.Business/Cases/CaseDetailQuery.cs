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
    /// The one case with its parent, or null where the tenant has no such case.
    /// </summary>
    public static async Task<CaseDetail?> DetailOf(this IQueryable<Case> cases, Guid caseId, CancellationToken token)
    {
        return await cases
            .WithId(caseId)
            .Select(static @case => new CaseDetail
            {
                CaseId = @case.Id,
                CaseNumber = @case.CaseNumber,
                ExternalCaseNumber = @case.ExternalCaseNumber,
                Date = @case.Date,
                Title = @case.Title,
                Description = @case.Description,
                Status = @case.Status,
                ParentCase = @case.ParentCase == null
                    ? null
                    : new CaseListItem
                    {
                        CaseId = @case.ParentCase.Id,
                        CaseNumber = @case.ParentCase.CaseNumber,
                        Title = @case.ParentCase.Title,
                        Date = @case.ParentCase.Date,
                        Status = @case.ParentCase.Status,
                        Changed = @case.ParentCase.Updated ?? @case.ParentCase.Created,
                    },
            })
            .SingleOrDefaultAsync(token);
    }
}

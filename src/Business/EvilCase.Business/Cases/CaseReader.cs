using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession dbSession) : ICaseReader
{
    public async Task<CaseListResponse> ListCases(CaseListRequest request, CancellationToken token)
    {
        var filtered = dbSession.Current.Cases
            .MatchingSearch(request.Search)
            .WithStatus(request.Status)
            .UnderParent(request.ParentCaseId, request.Scope)
            .WithContact(request.ContactId)
            .WithLabels(request.LabelIds)
            .WithinDates(request.From, request.To);

        var total = await filtered.CountAsync(token);

        var items = await filtered
            .InSortOrder(request.Sort, request.SortDirection)
            .InPage(request.Skip, request.Take)
            .AsListItems()
            .ToListAsync(token);

        return new CaseListResponse { Items = items, TotalCount = total };
    }

    public async Task<CaseStatusCounts> CountCasesByStatus(CancellationToken token)
    {
        var counted = await dbSession.Current.Cases
            .GroupBy(static @case => @case.Status)
            .Select(static group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(static row => row.Status, static row => row.Count, token);

        return new CaseStatusCounts
        {
            Active = counted.GetValueOrDefault(CaseStatus.Active),
            WaitingOnAuthority = counted.GetValueOrDefault(CaseStatus.WaitingOnAuthority),
            Closed = counted.GetValueOrDefault(CaseStatus.Closed),
        };
    }

    public async Task<CaseDetail?> GetCaseDetail(Guid caseId, CancellationToken token)
    {
        return await dbSession.Current.Cases.DetailOf(caseId, token);
    }
}

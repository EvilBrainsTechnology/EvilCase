using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession dbSession) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken token)
    {
        var cases = dbSession.Current.Cases
            .MatchingSearch(request.Search)
            .WithStatus(request.Status);

        var ordered = request.Order == CaseListOrder.Changed ? cases.InChangeOrder() : cases.InListOrder();

        return await ordered
            .TakeAtMost(request.Take)
            .AsListItems()
            .ToListAsync(token);
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
        var context = dbSession.Current;

        var @case = await context.Cases.DetailOf(caseId, token);
        if (@case is null)
            return null;

        var children = await context.Cases
            .WithParent(caseId)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);

        var numbers = await context.ExternalCaseNumbers
            .OfCase(caseId)
            .InAssignmentOrder()
            .AsItems()
            .ToListAsync(token);

        return @case with { ChildCases = children, ExternalNumbers = numbers };
    }
}

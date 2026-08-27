using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
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
        var cases = dbSession.Current.Cases;

        // The database counts, over the whole tenant, whatever a list request narrows to.
        var active = await cases
            .WithStatus(CaseStatusFilter.Active)
            .CountAsync(token);

        var waiting = await cases
            .WithStatus(CaseStatusFilter.WaitingOnAuthority)
            .CountAsync(token);

        var closed = await cases
            .WithStatus(CaseStatusFilter.Closed)
            .CountAsync(token);

        return new CaseStatusCounts { Active = active, WaitingOnAuthority = waiting, Closed = closed };
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

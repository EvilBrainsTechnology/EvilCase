using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ActReader(IDbSession dbSession) : IActReader
{
    private const int TimelineReach = 2;

    public async Task<ActListResponse> ListActs(ActListRequest request, CancellationToken token)
    {
        var filtered = dbSession.Current.Acts
            .MatchingSearch(request.Search)
            .OfCase(request.CaseId)
            .WithContact(request.ContactId)
            .WithDirection(request.Direction)
            .WithLabels(request.LabelIds)
            .WithinDates(request.From, request.To);

        var total = await filtered.CountAsync(token);

        var items = await filtered
            .InSortOrder(request.Sort, request.SortDirection)
            .InPage(request.Skip, request.Take)
            .AsListItems()
            .ToListAsync(token);

        return new ActListResponse { Items = items, TotalCount = total };
    }

    public async Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token)
    {
        var detail = await dbSession.Current.Acts.DetailOf(caseId, actId, token);

        if (detail is null)
            return null;

        var caseActs = await dbSession.Current.Acts
            .OfCase(caseId)
            .InSortOrder(ActSortKey.Date, ListSortDirection.Ascending)
            .AsTimelineItems(actId)
            .ToListAsync(token);

        var index = caseActs.FindIndex(static item => item.IsCurrent);
        var first = Math.Max(0, index - TimelineReach);
        var last = Math.Min(caseActs.Count - 1, index + TimelineReach);

        return detail with
        {
            ActPosition = index + 1,
            CaseActCount = caseActs.Count,
            CaseTimeline = caseActs.GetRange(first, last - first + 1),
        };
    }
}

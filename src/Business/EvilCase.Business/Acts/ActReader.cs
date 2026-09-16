using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ActReader(IDbSession dbSession) : IActReader
{
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
        return await dbSession.Current.Acts.DetailOf(caseId, actId, token);
    }
}

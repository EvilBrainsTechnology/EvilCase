using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ActReader(IDbSession dbSession) : IActReader
{
    public async Task<IReadOnlyList<ActListItem>> ListActs(ActListRequest request, CancellationToken token)
    {
        return await dbSession.Current.Acts
            .InLatestOrder()
            .TakeAtMost(request.Take)
            .AsListItems()
            .ToListAsync(token);
    }

    public async Task<IReadOnlyList<ActListItem>> ListCaseActs(Guid caseId, CancellationToken token)
    {
        return await dbSession.Current.Acts
            .OfCase(caseId)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    public async Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token)
    {
        var context = dbSession.Current;

        var act = await context.Acts.DetailOf(caseId, actId, token);
        if (act is null)
            return null;

        var numbers = await context.ExternalActNumbers
            .OfAct(caseId, actId)
            .InAssignmentOrder()
            .AsItems()
            .ToListAsync(token);

        return act with { ExternalNumbers = numbers };
    }
}

using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Acts;

internal sealed class ActReader(IDbSession dbSession) : IActReader
{
    public async Task<IReadOnlyList<ActListItem>> ListActs(Guid caseId, CancellationToken token)
    {
        return await dbSession.Current.Acts
            .OfCase(caseId)
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);
    }

    public Task<ActDetail?> GetActDetail(Guid caseId, Guid actId, CancellationToken token)
    {
        return dbSession.Current.Acts.DetailOf(caseId, actId, token);
    }
}

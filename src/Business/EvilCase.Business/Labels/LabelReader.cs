using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Labels;

internal sealed class LabelReader(IDbSession dbSession) : ILabelReader
{
    public async Task<IReadOnlyList<LabelItem>> ListLabels(CancellationToken token)
    {
        return await dbSession.Current.Labels
            .InListOrder()
            .AsListItems()
            .ToListAsync(token);
    }
}

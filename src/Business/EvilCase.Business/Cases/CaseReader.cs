using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession session) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default) =>
        await session.Current.Cases
            .MatchingSearch(request.Search)
            .WithStatus(request.Status)
            .InListOrder()
            .AsListItems()
            .ToListAsync(cancellationToken);
}

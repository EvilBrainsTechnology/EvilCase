using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession dbSession) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> ListCases(CaseListRequest request, CancellationToken cancellationToken)
    {
        return await dbSession.Current.Cases
            .MatchingSearch(request.Search)
            .WithStatus(request.Status)
            .InListOrder()
            .AsListItems()
            .ToListAsync(cancellationToken);
    }
}

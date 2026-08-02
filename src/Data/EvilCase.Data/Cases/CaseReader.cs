using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Cases;

internal sealed class CaseReader(ApplicationDbContext context) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await context.Cases
            .Roots()
            .MatchingSearch(request.Search)
            .WithStatus(request.Status)
            .InListOrder()
            .AsListItems()
            .ToListAsync(cancellationToken);
    }
}

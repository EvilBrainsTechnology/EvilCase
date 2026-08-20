using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbContextAccessor accessor) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await Compose(accessor, request).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Internal so a test reads the SQL the reader really runs.
    /// </summary>
    internal static IQueryable<CaseListItem> Compose(IDbContextAccessor accessor, CaseListRequest request) => accessor.Current.Set<Case>()
        .MatchingSearch(request.Search)
        .WithStatus(request.Status)
        .InListOrder()
        .AsListItems();
}

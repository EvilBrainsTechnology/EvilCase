using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession session) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await Compose(session.Current, request.Search).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Internal so a test reads the SQL the reader really runs.
    /// </summary>
    internal static IQueryable<CaseListItem> Compose(ApplicationDbContext context, string? search = null) => context.Cases
        .MatchingSearch(search)
        .InListOrder()
        .AsListItems();
}

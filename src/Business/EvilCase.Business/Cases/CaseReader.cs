using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(ApplicationDbContext context) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await Compose(context, request).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Every step the list is made of, so a test reads the SQL the reader really runs.
    /// </summary>
    internal static IQueryable<CaseListItem> Compose(ApplicationDbContext context, CaseListRequest request) => context.Cases
        .MatchingSearch(request.Search)
        .WithStatus(request.Status)
        .InListOrder()
        .AsListItems();
}

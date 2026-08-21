using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession session) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CancellationToken cancellationToken = default) =>
        await Compose(session.Current).ToListAsync(cancellationToken);

    /// <summary>
    /// Internal so a test reads the SQL the reader really runs.
    /// </summary>
    internal static IQueryable<CaseListItem> Compose(ApplicationDbContext context) => context.Cases
        .InListOrder()
        .AsListItems();
}

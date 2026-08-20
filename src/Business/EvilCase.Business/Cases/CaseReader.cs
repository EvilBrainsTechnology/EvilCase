using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Sessions;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IApplicationDbSession session) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        return await Compose(session, request).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Internal so a test reads the SQL the reader really runs.
    /// </summary>
    internal static IQueryable<CaseListItem> Compose(IApplicationDbSession session, CaseListRequest request) => session.Query<Case>()
        .MatchingSearch(request.Search)
        .WithStatus(request.Status)
        .InListOrder()
        .AsListItems();
}

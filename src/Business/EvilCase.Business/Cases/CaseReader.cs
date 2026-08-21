using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

internal sealed class CaseReader(IDbSession session) : ICaseReader
{
    public async Task<IReadOnlyList<CaseListItem>> List(CaseListRequest request, CancellationToken cancellationToken = default)
    {
        return await session.Current.Cases
            .MatchingSearch(request.Search)
            .WithStatus(request.Status)
            .InListOrder()
            .AsListItems()
            .ToListAsync(cancellationToken);
    }

    public async Task<CaseDetail?> Detail(Guid id, CancellationToken cancellationToken = default) =>
        await Compose(session.Current.Cases, id).FirstOrDefaultAsync(cancellationToken);

    internal static IQueryable<CaseDetail> Compose(IQueryable<Case> cases, Guid id) =>
        cases.Where(@case => @case.Id == id).AsDetails();
}

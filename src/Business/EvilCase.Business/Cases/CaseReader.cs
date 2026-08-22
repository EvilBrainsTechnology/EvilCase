using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
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

    public async Task<CaseDetail?> Detail(Guid id, CancellationToken cancellationToken = default)
    {
        return await session.Current.Cases
            .Where(@case => @case.Id == id)
            .Select(@case => new CaseDetail
            {
                Id = @case.Id,
                CaseNumber = @case.CaseNumber,
                Date = @case.Date,
                Title = @case.Title,
                Description = @case.Description,
                Status = @case.Status,
            })
            .FirstOrDefaultAsync(cancellationToken);
    }
}

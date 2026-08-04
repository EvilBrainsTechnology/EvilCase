using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Cases;

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

    public async Task<CaseDetailResponse?> Detail(long id, CancellationToken cancellationToken = default)
    {
        var detail = await context.Cases
            .WithId(id)
            .AsDetails()
            .FirstOrDefaultAsync(cancellationToken);

        if (detail is null)
            return null;

        var graph = await context.Cases
            .AroundCase(id)
            .AsGraphNodes()
            .ToListAsync(cancellationToken);

        var comments = await context.Comments
            .OnCase(id)
            .InDiaryOrder()
            .AsCaseComments()
            .ToListAsync(cancellationToken);

        return detail with
        {
            Ancestors = CaseGraph.Ancestors(graph, id),
            SubCases = CaseGraph.SubCases(graph, id),
            Comments = comments,
        };
    }
}

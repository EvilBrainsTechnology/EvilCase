using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Search;

internal sealed class SearchReader(IDbSession dbSession) : ISearchReader
{
    private const int MaxResults = 10;

    private const int MinimumTermLength = 2;

    public async Task<SearchResponse> Search(SearchRequest request, CancellationToken token)
    {
        var term = request.Query?.Trim() ?? "";
        if (term.Length < MinimumTermLength)
            return new SearchResponse { Items = [] };

        var context = dbSession.Current;

        var cases = await context.Cases
            .MatchingTerm(term)
            .InSearchOrder()
            .AsSearchItems()
            .Take(MaxResults)
            .ToListAsync(token);

        var acts = await context.Acts
            .MatchingTerm(term)
            .InSearchOrder()
            .AsSearchItems()
            .Take(MaxResults)
            .ToListAsync(token);

        var items = cases
            .Concat(acts)
            .OrderByDescending(item => item.Date)
            .ThenBy(item => item.Number, StringComparer.Ordinal)
            .Take(MaxResults)
            .ToList();

        return new SearchResponse { Items = items, ExactMatch = await this.ResolveExactMatch(term, token) };
    }

    /// <summary>
    /// The entity Enter opens: an own number always, an external one only where a single entity carries
    /// it (SDD-014).
    /// </summary>
    private async Task<SearchResultItem?> ResolveExactMatch(string term, CancellationToken token)
    {
        var context = dbSession.Current;
        var exact = term.EscapeLikeWildcards();

        var @case = await context.Cases
            .Where(candidate => EF.Functions.ILike(candidate.CaseNumber, exact, LikeExtensions.LikeEscape))
            .AsSearchItems()
            .SingleOrDefaultAsync(token);

        if (@case is not null)
            return @case;

        var act = await context.Acts
            .Where(candidate => EF.Functions.ILike(candidate.ActNumber, exact, LikeExtensions.LikeEscape))
            .AsSearchItems()
            .SingleOrDefaultAsync(token);

        if (act is not null)
            return act;

        var byCaseMark = await context.ExternalCaseNumbers
            .Where(number => EF.Functions.ILike(number.Value, exact, LikeExtensions.LikeEscape))
            .Select(number => number.Case!)
            .AsSearchItems()
            .Take(2)
            .ToListAsync(token);

        var byActMark = await context.ExternalActNumbers
            .Where(number => EF.Functions.ILike(number.Value, exact, LikeExtensions.LikeEscape))
            .Select(number => number.Act!)
            .AsSearchItems()
            .Take(2)
            .ToListAsync(token);

        return byCaseMark.Count + byActMark.Count == 1 ? byCaseMark.Concat(byActMark).Single() : null;
    }
}

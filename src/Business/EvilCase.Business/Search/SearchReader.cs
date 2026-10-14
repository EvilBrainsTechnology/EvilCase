using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Search;

internal sealed class SearchReader(IDbSession dbSession) : ISearchReader
{
    public async Task<SearchResponse> Search(SearchRequest request, CancellationToken token)
    {
        var context = dbSession.Current;
        var pattern = request.Q.ContainsLikePattern();

        // Sequential: one DbContext serves one query at a time.
        return new SearchResponse
        {
            Cases = await ReadGroup(context.Cases.MatchingTerm(pattern).InSearchOrder(), static cases => cases.AsListItems(), token),
            Acts = await ReadGroup(context.Acts.MatchingTerm(pattern).InSearchOrder(), static acts => acts.AsListItems(), token),
            Contacts = await ReadGroup(context.Contacts.MatchingTerm(pattern).InSearchOrder(), static contacts => contacts.AsListItems(), token),
            Files = await ReadGroup(context.FileAssets.MatchingTerm(pattern).InSearchOrder(), static files => files.AsSearchItems(), token),
            Comments = await ReadGroup(context.Comments.MatchingTerm(pattern).InSearchOrder(), static comments => comments.AsSearchItems(), token),
            Labels = await ReadGroup(context.Labels.MatchingTerm(pattern).InSearchOrder(), static labels => labels.AsCountedListItems(), token),
        };
    }

    private static async Task<SearchGroup<TItem>> ReadGroup<TEntity, TItem>(
        IQueryable<TEntity> ordered,
        Func<IQueryable<TEntity>, IQueryable<TItem>> project,
        CancellationToken token)
    {
        var total = await ordered.CountAsync(token);
        var items = await project(ordered.InPage(0, SearchLimits.GroupSize)).ToListAsync(token);

        return new SearchGroup<TItem> { Items = items, TotalCount = total };
    }
}

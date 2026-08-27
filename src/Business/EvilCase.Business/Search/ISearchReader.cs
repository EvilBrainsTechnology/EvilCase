using EvilBrains.EvilCase.Api.Contract.Search;

namespace EvilBrains.EvilCase.Business.Search;

/// <summary>
/// Answers one search over cases and acts (SDD-014).
/// </summary>
public interface ISearchReader
{
    public Task<SearchResponse> Search(SearchRequest request, CancellationToken token);
}

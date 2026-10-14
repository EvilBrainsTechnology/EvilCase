using EvilBrains.EvilCase.Api.Contract.Search;

namespace EvilBrains.EvilCase.Business.Search;

public interface ISearchReader
{
    public Task<SearchResponse> Search(SearchRequest request, CancellationToken token);
}

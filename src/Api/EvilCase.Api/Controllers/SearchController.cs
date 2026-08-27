using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Business.Search;
using Microsoft.AspNetCore.Mvc;

namespace EvilBrains.EvilCase.Api.Controllers;

[ApiController]
[GenerateApiClient]
[Route("api/search")]
public class SearchController : ControllerBase
{
    [HttpGet("")]
    public Task<SearchResponse> Search([FromServices] ISearchReader search, [FromQuery] SearchRequest request, CancellationToken token)
    {
        return search.Search(request, token);
    }
}

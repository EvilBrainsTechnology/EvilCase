using System.ComponentModel;
using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Business.Search;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

[McpServerToolType]
internal static class SearchTools
{
    [McpServerTool(Name = "search", ReadOnly = true, OpenWorld = false)]
    [Description(
        "Searches cases, acts, contacts, file names and comments at once. q is 2 to 256 characters with no space at either end. "
            + "Each group carries at most 3 hits and its total count; the list tools page through the rest.")]
    public static async Task<SearchResponse> Search(ISearchReader search, [Description("The term.")] SearchRequest request, CancellationToken token)
    {
        McpProblem.Validate(request);

        return await search.Search(request, token);
    }
}

using System.Security.Claims;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.ApiTokens;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace EvilBrains.EvilCase.Api.Mcp;

/// <summary>
/// A read-only token sees and calls only the tools annotated read-only; a tool without the annotation counts as a write.
/// </summary>
internal static class McpScopeFilters
{
    public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> ListTools(McpRequestHandler<ListToolsRequestParams, ListToolsResult> next)
    {
        return async (context, token) =>
        {
            var result = await next(context, token);

            if (!CanWrite(context.User))
                result.Tools = [.. result.Tools.Where(static tool => tool.Annotations?.ReadOnlyHint == true)];

            return result;
        };
    }

    public static McpRequestHandler<CallToolRequestParams, CallToolResult> CallTool(McpRequestHandler<CallToolRequestParams, CallToolResult> next)
    {
        return async (context, token) =>
        {
            var tools = context.Server.ServerOptions.ToolCollection;

            if (!CanWrite(context.User)
                && tools is not null
                && tools.TryGetPrimitive(context.Params.Name, out var tool)
                && tool.ProtocolTool.Annotations?.ReadOnlyHint != true)
            {
                return new CallToolResult
                {
                    IsError = true,
                    Content = [new TextContentBlock { Text = "Forbidden: A read-only API token cannot call a write tool." }],
                };
            }

            return await next(context, token);
        };
    }

    private static bool CanWrite(ClaimsPrincipal? user)
    {
        return string.Equals(user?.FindFirstValue(AuthClaims.ApiTokenScope), nameof(ApiTokenScope.ReadWrite), StringComparison.Ordinal);
    }
}

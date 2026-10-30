using System.Security.Claims;
using EvilBrains.EvilCase.Api.Contract.ApiTokens;
using EvilBrains.EvilCase.Api.Contract.Mcp;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Domain.ApiTokens;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Runs after authentication on every path, anonymous endpoints included: a token never reaches sign-in or
/// its own management, and a read-only token never writes through the REST API. Everything a token's request logs carries the
/// token's id and name.
/// </summary>
internal sealed class ApiTokenBoundaryMiddleware(RequestDelegate next, ILogger<ApiTokenBoundaryMiddleware> logger)
{
    public async Task Invoke(HttpContext context)
    {
        var user = context.User;

        if (user.FindFirstValue(AuthClaims.ApiTokenId) is not { } apiTokenId)
        {
            await next(context);
            return;
        }

        using var scope = logger.BeginScope(new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["ApiTokenId"] = apiTokenId,
            ["ApiTokenName"] = user.FindFirstValue(AuthClaims.ApiTokenName),
        });

        if (Refusal(context.Request, user) is not { } detail)
        {
            await next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;

        var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();

        await problemDetails.TryWriteAsync(new()
        {
            HttpContext = context,
            ProblemDetails =
            {
                Status = StatusCodes.Status403Forbidden,
                Title = "Forbidden",
                Detail = detail,
            },
        });
    }

    private static string? Refusal(HttpRequest request, ClaimsPrincipal user)
    {
        if (request.Path.StartsWithSegments(AuthRoute.Path, StringComparison.OrdinalIgnoreCase)
            || request.Path.StartsWithSegments(ApiTokenRoute.Path, StringComparison.OrdinalIgnoreCase))
        {
            return "An API token cannot reach sign-in, sessions or API token management.";
        }

        // Every MCP message is a POST; the MCP server holds a read-only token to its read tools itself.
        if (request.Path.StartsWithSegments(McpRoute.Path, StringComparison.OrdinalIgnoreCase))
            return null;

        var write = !HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method) && !HttpMethods.IsOptions(request.Method);

        if (write && !string.Equals(user.FindFirstValue(AuthClaims.ApiTokenScope), nameof(ApiTokenScope.ReadWrite), StringComparison.Ordinal))
            return "A read-only API token cannot write.";

        return null;
    }
}

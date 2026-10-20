namespace EvilBrains.EvilCase.Host;

/// <summary>
/// Rejects an API write the browser says came from another origin. The auth cookies are SameSite=Strict;
/// this is the second line. A request with neither <c>Sec-Fetch-Site</c> nor <c>Origin</c> is not from a
/// browser and passes.
/// </summary>
internal sealed class CrossSiteWriteMiddleware(RequestDelegate next)
{
    private const string SameOrigin = "same-origin";

    public async Task Invoke(HttpContext context)
    {
        if (IsApiWrite(context.Request) && !IsSameOrigin(context.Request))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;

            var problemDetails = context.RequestServices.GetRequiredService<IProblemDetailsService>();

            await problemDetails.TryWriteAsync(new()
            {
                HttpContext = context,
                ProblemDetails =
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "Forbidden",
                    Detail = "Cross-site requests are not accepted.",
                },
            });

            return;
        }

        await next(context);
    }

    private static bool IsApiWrite(HttpRequest request)
    {
        return request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            && !HttpMethods.IsGet(request.Method)
            && !HttpMethods.IsHead(request.Method)
            && !HttpMethods.IsOptions(request.Method);
    }

    private static bool IsSameOrigin(HttpRequest request)
    {
        if (request.Headers.TryGetValue("Sec-Fetch-Site", out var site) && site.Count > 0)
            return string.Equals(site.ToString(), SameOrigin, StringComparison.OrdinalIgnoreCase);

        if (!request.Headers.TryGetValue("Origin", out var origin) || origin.Count == 0)
            return true;

        return string.Equals(origin.ToString(), $"{request.Scheme}://{request.Host}", StringComparison.OrdinalIgnoreCase);
    }
}

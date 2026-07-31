using Microsoft.AspNetCore.Builder;
using Serilog;

namespace EvilBrains.Logging.AspNetCore;

public static class RequestLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Request context logging followed by Serilog's request logging, in that order, so the request
    /// completion event carries the identifiers as well. Only requests under
    /// <paramref name="loggedPaths"/> leave a completion log; the quiet paths are the exceptions
    /// inside them, and server errors are logged regardless of path.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(
        this IApplicationBuilder app,
        IReadOnlyList<string> loggedPaths,
        IReadOnlyList<string> quietPaths)
    {
        ArgumentNullException.ThrowIfNull(app);

        var policy = new RequestLogLevelPolicy(loggedPaths, quietPaths);

        app.UseRequestContextLogging();

        return app.UseSerilogRequestLogging(options => options.GetLevel = policy.GetLevel);
    }
}

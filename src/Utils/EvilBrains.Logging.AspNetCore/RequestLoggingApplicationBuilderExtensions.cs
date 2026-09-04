using Microsoft.AspNetCore.Builder;
using Serilog;

namespace EvilBrains.Logging.AspNetCore;

public static class RequestLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// UseRequestContextLogging goes first so the completion event carries the identifiers.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(
        this IApplicationBuilder app,
        IReadOnlyList<string> loggedPaths,
        IReadOnlyList<string> quietPaths)
    {
        var policy = new RequestLogLevelPolicy(loggedPaths, quietPaths);

        app.UseRequestContextLogging();

        return app.UseSerilogRequestLogging(options => options.GetLevel = policy.GetLevel);
    }
}

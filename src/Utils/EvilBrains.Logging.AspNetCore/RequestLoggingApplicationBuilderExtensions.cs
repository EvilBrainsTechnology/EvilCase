using Microsoft.AspNetCore.Builder;
using Serilog;

namespace EvilBrains.Logging.AspNetCore;

public static class RequestLoggingApplicationBuilderExtensions
{
    /// <summary>
    /// Request context logging followed by Serilog's request logging, in that order, so the request
    /// completion event carries the identifiers as well. Successful requests to the quiet paths and
    /// successful CORS preflights leave no completion log.
    /// </summary>
    public static IApplicationBuilder UseRequestLogging(this IApplicationBuilder app, params string[] quietPaths)
    {
        ArgumentNullException.ThrowIfNull(app);

        var policy = new RequestLogLevelPolicy(quietPaths);

        app.UseRequestContextLogging();

        return app.UseSerilogRequestLogging(options => options.GetLevel = policy.GetLevel);
    }
}

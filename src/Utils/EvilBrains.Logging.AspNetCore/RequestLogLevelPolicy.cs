using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Serilog's default request logging levels, except that successful CORS preflights and successful
/// requests to the quiet paths are demoted below the configured minimum, so they leave no log.
/// </summary>
internal sealed class RequestLogLevelPolicy(params string[] quietPaths)
{
    private readonly PathString[] quietPaths = [.. quietPaths.Select(x => new PathString(x))];

    public LogEventLevel GetLevel(HttpContext context, double _, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode > 499)
            return LogEventLevel.Error;

        if (context.Response.StatusCode < 400 && this.IsNoise(context.Request))
            return LogEventLevel.Verbose;

        return LogEventLevel.Information;
    }

    private bool IsNoise(HttpRequest request) =>
        HttpMethods.IsOptions(request.Method) || this.quietPaths.Any(x => request.Path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase));
}

using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Verbose is "no log" only while the host minimum sits above it. A successful upload of client logs is never
/// logged: the next upload would ship that log and log again.
/// </summary>
internal sealed class RequestLogLevelPolicy(IReadOnlyList<string> loggedPaths, IReadOnlyList<string> quietPaths)
{
    private readonly PathString[] logged = [.. loggedPaths.Select(static x => new PathString(x))];

    private readonly PathString[] quiet = [.. quietPaths.Select(static x => new PathString(x))];

    public LogEventLevel GetLevel(HttpContext context, double _, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode > 499)
            return LogEventLevel.Error;

        return this.IsLogged(context.Request, context.Response.StatusCode) ? LogEventLevel.Information : LogEventLevel.Verbose;
    }

    private bool IsLogged(HttpRequest request, int statusCode)
    {
        return !HttpMethods.IsOptions(request.Method)
            && StartsWithAny(request.Path, this.logged)
            && (statusCode >= 400 || !StartsWithAny(request.Path, this.quiet));
    }

    private static bool StartsWithAny(PathString path, PathString[] prefixes)
    {
        return Array.Exists(prefixes, x => path.StartsWithSegments(x, StringComparison.OrdinalIgnoreCase));
    }
}

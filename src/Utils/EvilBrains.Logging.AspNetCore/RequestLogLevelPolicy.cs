using Microsoft.AspNetCore.Http;
using Serilog.Events;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Serilog's default request logging levels, narrowed to the logged paths: anything outside them —
/// a static asset, the frontend itself, a health probe — is demoted below the configured minimum and
/// leaves no log, and so is a successful request to a quiet path inside them, or a CORS preflight.
/// Failures are logged wherever they happen: server errors as errors, and a rejected request to a
/// quiet path as information, because it is the upload that succeeds which must not feed itself.
/// </summary>
internal sealed class RequestLogLevelPolicy(IReadOnlyList<string> loggedPaths, IReadOnlyList<string> quietPaths)
{
    private readonly PathString[] logged = [.. loggedPaths.Select(x => new PathString(x))];

    private readonly PathString[] quiet = [.. quietPaths.Select(x => new PathString(x))];

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

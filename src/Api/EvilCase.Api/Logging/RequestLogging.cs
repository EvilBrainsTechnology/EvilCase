using Serilog.Events;

namespace EvilBrains.EvilCase.Api.Logging;

internal static class RequestLogging
{
    private static readonly PathString ClientLogPath = new("/logs/client");

    /// <summary>
    /// Serilog's default levels, except that successful log uploads and CORS preflights are demoted
    /// below the configured minimum. At a one second batch interval and a preflight per request they
    /// would drown out everything else.
    /// </summary>
    public static LogEventLevel GetLevel(HttpContext context, double _, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode > 499)
            return LogEventLevel.Error;

        if (context.Response.StatusCode < 400 && IsNoise(context.Request))
            return LogEventLevel.Verbose;

        return LogEventLevel.Information;
    }

    private static bool IsNoise(HttpRequest request) =>
        HttpMethods.IsOptions(request.Method) || request.Path.StartsWithSegments(ClientLogPath, StringComparison.OrdinalIgnoreCase);
}

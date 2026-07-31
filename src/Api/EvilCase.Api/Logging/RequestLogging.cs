using Serilog.Events;

namespace EvilBrains.EvilCase.Api.Logging;

internal static class RequestLogging
{
    private static readonly PathString ClientLogPath = new("/logs/client");

    /// <summary>
    /// Serilog's default levels, except that a successful log upload is demoted below the configured
    /// minimum: at a one second batch interval its completion events would drown out everything else.
    /// </summary>
    public static LogEventLevel GetLevel(HttpContext context, double _, Exception? exception)
    {
        if (exception is not null || context.Response.StatusCode > 499)
            return LogEventLevel.Error;

        if (context.Response.StatusCode < 400 && context.Request.Path.StartsWithSegments(ClientLogPath, StringComparison.OrdinalIgnoreCase))
            return LogEventLevel.Verbose;

        return LogEventLevel.Information;
    }
}

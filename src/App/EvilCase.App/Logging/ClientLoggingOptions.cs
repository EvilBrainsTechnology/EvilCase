using Serilog.Events;

namespace EvilBrains.EvilCase.App.Logging;

/// <summary>
/// Minimum levels for the browser console and for the events shipped to the API.
/// </summary>
internal sealed record ClientLoggingOptions
{
    private const string SectionName = "ClientLogging";

    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;

    public LogEventLevel ServerMinimumLevel { get; init; } = LogEventLevel.Warning;

    public static ClientLoggingOptions Read(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);

        return new()
        {
            MinimumLevel = Parse(section["MinimumLevel"], LogEventLevel.Information),
            ServerMinimumLevel = Parse(section["ServerMinimumLevel"], LogEventLevel.Warning),
        };
    }

    private static LogEventLevel Parse(string? value, LogEventLevel fallback) =>
        Enum.TryParse<LogEventLevel>(value, ignoreCase: true, out var level) ? level : fallback;
}

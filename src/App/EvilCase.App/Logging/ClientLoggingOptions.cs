using Serilog.Events;

namespace EvilBrains.EvilCase.App.Logging;

/// <summary>
/// Minimum levels for the browser console and for the events shipped to the API.
/// </summary>
internal sealed record ClientLoggingOptions
{
    public const string SectionName = "ClientLogging";

    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;

    public LogEventLevel ServerMinimumLevel { get; init; } = LogEventLevel.Warning;
}

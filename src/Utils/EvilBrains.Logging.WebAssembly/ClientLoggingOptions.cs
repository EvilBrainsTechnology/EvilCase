using Serilog.Events;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Minimum levels for the browser console and for the events shipped to the server.
/// </summary>
internal sealed record ClientLoggingOptions
{
    public LogEventLevel MinimumLevel { get; init; } = LogEventLevel.Information;

    public LogEventLevel ServerMinimumLevel { get; init; } = LogEventLevel.Warning;
}

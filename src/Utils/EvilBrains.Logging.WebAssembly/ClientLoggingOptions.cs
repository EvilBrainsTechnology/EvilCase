using Serilog.Events;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Minimum levels for the browser console and for the events shipped to the server.
/// </summary>
internal sealed record ClientLoggingOptions
{
    // Settable, not init: the configuration binding source generator assigns after construction and
    // silently skips init-only properties, which would leave the defaults in place.
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    public LogEventLevel ServerMinimumLevel { get; set; } = LogEventLevel.Warning;
}

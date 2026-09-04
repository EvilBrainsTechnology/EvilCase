using Serilog.Events;

namespace EvilBrains.Logging.WebAssembly;

internal sealed record ClientLoggingOptions
{
    // Settable, not init: the configuration binding source generator assigns after construction and
    // silently skips init-only properties, which would leave the defaults in place.
    public LogEventLevel MinimumLevel { get; set; } = LogEventLevel.Information;

    public LogEventLevel ServerMinimumLevel { get; set; } = LogEventLevel.Warning;
}

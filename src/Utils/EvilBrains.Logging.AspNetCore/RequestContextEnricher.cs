using Serilog.Core;
using Serilog.Events;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Puts the identifiers of the current request on the event, overwriting what is already there.
/// Pushing them as plain log context properties is not enough: ASP.NET Core opens a logging scope
/// per request that defines RequestId as the TraceIdentifier, and a scope property reaches the event
/// before the log context does, so every entry written through ILogger&lt;T&gt; would carry the
/// connection-local identifier instead of the one the caller sent.
/// </summary>
internal sealed class RequestContextEnricher(IReadOnlyList<LogEventProperty> properties) : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        foreach (var property in properties)
            logEvent.AddOrUpdateProperty(property);
    }
}

using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using Serilog.Events;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Puts the request, correlation, session and machine identifiers of the incoming request into the
/// Serilog log context, so every event written while the request runs carries them.
/// </summary>
internal sealed class RequestContextLoggingMiddleware(RequestDelegate next)
{
    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // Falling back to the trace identifier keeps events of an unidentified caller groupable.
        var requestId = ReadId(context, RequestContextHeaderNames.RequestId) ?? context.TraceIdentifier;

        var properties = new List<LogEventProperty> { new(RequestContextPropertyNames.RequestId, new ScalarValue(requestId)) };

        Add(properties, context, RequestContextHeaderNames.CorrelationId, RequestContextPropertyNames.CorrelationId);
        Add(properties, context, RequestContextHeaderNames.SessionId, RequestContextPropertyNames.SessionId);
        Add(properties, context, RequestContextHeaderNames.MachineId, RequestContextPropertyNames.MachineId);

        using (LogContext.Push(new RequestContextEnricher(properties)))
            await next(context);
    }

    /// <summary>
    /// A caller that sends no identifier gets no property; an "unknown" placeholder would only pollute queries.
    /// </summary>
    private static void Add(List<LogEventProperty> properties, HttpContext context, string headerName, string propertyName)
    {
        if (ReadId(context, headerName) is { } id)
            properties.Add(new(propertyName, new ScalarValue(id)));
    }

    /// <summary>
    /// Headers are untrusted: only a single well-formed value is accepted, and the identifier is
    /// re-formatted rather than logged as received.
    /// </summary>
    private static string? ReadId(HttpContext context, string headerName) =>
        context.Request.Headers.TryGetValue(headerName, out var values) && values.Count == 1 && Guid.TryParse(values[0], out var id)
            ? id.ToString("D", CultureInfo.InvariantCulture)
            : null;
}

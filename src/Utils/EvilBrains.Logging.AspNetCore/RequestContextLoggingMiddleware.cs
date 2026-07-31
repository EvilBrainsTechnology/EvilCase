using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace EvilBrains.Logging.AspNetCore;

/// <summary>
/// Puts the identifiers of the incoming request into the Serilog log context, so every event written
/// while the request runs carries them.
/// </summary>
internal sealed class RequestContextLoggingMiddleware(RequestDelegate next)
{
    /// <summary>
    /// The name of the scope ASP.NET Core opens per request. It is pushed here as well, because that
    /// scope only reaches what is logged through <c>ILogger&lt;T&gt;</c> and Serilog writes its own
    /// request completion event outside it.
    /// </summary>
    private const string TraceIdentifierPropertyName = "RequestId";

    public async Task Invoke(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        using (LogContext.PushProperty(TraceIdentifierPropertyName, context.TraceIdentifier))
        using (Push(context, RequestContextHeaderNames.RequestId, RequestContextPropertyNames.RequestId))
        using (Push(context, RequestContextHeaderNames.CorrelationId, RequestContextPropertyNames.CorrelationId))
        using (Push(context, RequestContextHeaderNames.SessionId, RequestContextPropertyNames.SessionId))
        using (Push(context, RequestContextHeaderNames.MachineId, RequestContextPropertyNames.MachineId))
            await next(context);
    }

    /// <summary>
    /// A caller that sends no identifier gets no property; an "unknown" placeholder would only pollute queries.
    /// </summary>
    private static IDisposable Push(HttpContext context, string headerName, string propertyName)
    {
        var id = ReadId(context, headerName);

        return id is null ? LogContext.Push() : LogContext.PushProperty(propertyName, id);
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

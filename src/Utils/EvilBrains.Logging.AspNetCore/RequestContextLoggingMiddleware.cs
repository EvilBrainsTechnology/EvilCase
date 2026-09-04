using EvilBrains.Logging.Contract;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace EvilBrains.Logging.AspNetCore;

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
        using (LogContext.PushProperty(TraceIdentifierPropertyName, context.TraceIdentifier))
        using (Push(context, RequestContextHeaderNames.RequestId, RequestContextPropertyNames.RequestId))
        using (Push(context, RequestContextHeaderNames.CorrelationId, RequestContextPropertyNames.CorrelationId))
        using (Push(context, RequestContextHeaderNames.SessionId, RequestContextPropertyNames.SessionId))
        using (Push(context, RequestContextHeaderNames.MachineId, RequestContextPropertyNames.MachineId))
            await next(context);
    }

    private static IDisposable Push(HttpContext context, string headerName, string propertyName)
    {
        var id = ReadId(context, headerName);

        return id is null ? LogContext.Push() : LogContext.PushProperty(propertyName, id);
    }

    /// <summary>
    /// Untrusted: a repeated or malformed header is dropped, never logged as received.
    /// </summary>
    private static string? ReadId(HttpContext context, string headerName)
    {
        return context.Request.Headers.TryGetValue(headerName, out var values) && values.Count == 1 && Guid.TryParse(values[0], out var id)
            ? id.ToString("D", CultureInfo.InvariantCulture)
            : null;
    }
}

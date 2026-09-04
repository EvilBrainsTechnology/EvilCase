using EvilBrains.Logging.Contract;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// A successful upload of a log batch is not logged: the next upload would ship that log and log again.
/// A failed one settles because the batch is dropped.
/// </summary>
internal sealed class ClientHttpLogger(ILogger<ClientHttpLogger> logger, string quietPath) : IHttpClientLogger
{
    public object? LogRequestStart(HttpRequestMessage request)
    {
        return null;
    }

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        if (response.IsSuccessStatusCode && this.IsQuiet(request))
            return;

        var method = request.Method.Method;
        var path = Path(request);
        var statusCode = (int)response.StatusCode;
        var milliseconds = Milliseconds(elapsed);

        using var scope = logger.BeginScope(Identifiers(request));

        logger.LogInformation(
            "HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {Elapsed} ms",
            method,
            path,
            statusCode,
            milliseconds);
    }

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        var method = request.Method.Method;
        var path = Path(request);
        var milliseconds = Milliseconds(elapsed);

        using var scope = logger.BeginScope(Identifiers(request));

        logger.LogWarning(
            exception,
            "HTTP {HttpMethod} {RequestPath} failed after {Elapsed} ms",
            method,
            path,
            milliseconds);
    }

    private static Dictionary<string, object?> Identifiers(HttpRequestMessage request)
    {
        return new(StringComparer.Ordinal)
        {
            [RequestContextPropertyNames.RequestId] = Header(request, RequestContextHeaderNames.RequestId),
            [RequestContextPropertyNames.CorrelationId] = Header(request, RequestContextHeaderNames.CorrelationId),
        };
    }

    private static double Milliseconds(in TimeSpan elapsed)
    {
        return Math.Round(elapsed.TotalMilliseconds, 1, MidpointRounding.AwayFromZero);
    }

    private static string Path(HttpRequestMessage request)
    {
        return request.RequestUri?.AbsolutePath ?? "";
    }

    private static string? Header(HttpRequestMessage request, string name)
    {
        return request.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    /// <summary>
    /// A suffix match, not an equality one: the app may be served from a sub-path, which the base address
    /// carries into the resolved URI. The leading slash of the quiet path keeps the match on a segment boundary.
    /// </summary>
    private bool IsQuiet(HttpRequestMessage request)
    {
        return request.RequestUri?.AbsolutePath.EndsWith(quietPath, StringComparison.OrdinalIgnoreCase) == true;
    }
}

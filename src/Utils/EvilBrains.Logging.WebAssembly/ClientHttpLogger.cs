using EvilBrains.Logging.Contract;
using Microsoft.Extensions.Http.Logging;
using Microsoft.Extensions.Logging;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Replaces the four events the HTTP client factory logs per request with one, carrying the
/// identifiers the request was stamped with, so a browser call and its server side share a RequestId.
/// A successful upload of the log batch is not logged at all: it would be shipped by the next upload,
/// which would log again. A failed one is, and it settles because a batch that fails is dropped.
/// </summary>
internal sealed partial class ClientHttpLogger(ILogger<ClientHttpLogger> logger, string quietPath) : IHttpClientLogger
{
    public object? LogRequestStart(HttpRequestMessage request) => null;

    public void LogRequestStop(object? context, HttpRequestMessage request, HttpResponseMessage response, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(response);

        if (this.IsQuiet(request))
            return;

        var method = request.Method.Method;
        var path = Path(request);
        var statusCode = (int)response.StatusCode;
        var milliseconds = Milliseconds(elapsed);

        using var scope = logger.BeginScope(Identifiers(request));

        RequestCompleted(logger, method, path, statusCode, milliseconds);
    }

    public void LogRequestFailed(object? context, HttpRequestMessage request, HttpResponseMessage? response, Exception exception, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(request);

        var method = request.Method.Method;
        var path = Path(request);
        var milliseconds = Milliseconds(elapsed);

        using var scope = logger.BeginScope(Identifiers(request));

        RequestFailed(logger, exception, method, path, milliseconds);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "HTTP {HttpMethod} {RequestPath} responded {StatusCode} in {Elapsed} ms")]
    private static partial void RequestCompleted(ILogger logger, string httpMethod, string requestPath, int statusCode, double elapsed);

    [LoggerMessage(Level = LogLevel.Warning, Message = "HTTP {HttpMethod} {RequestPath} failed after {Elapsed} ms")]
    private static partial void RequestFailed(ILogger logger, Exception exception, string httpMethod, string requestPath, double elapsed);

    /// <summary>
    /// The identifiers ride in a scope rather than in the message: they are for correlating, not for reading.
    /// </summary>
    private static Dictionary<string, object?> Identifiers(HttpRequestMessage request) => new(StringComparer.Ordinal)
    {
        [RequestContextPropertyNames.RequestId] = Header(request, RequestContextHeaderNames.RequestId),
        [RequestContextPropertyNames.CorrelationId] = Header(request, RequestContextHeaderNames.CorrelationId),
    };

    private static double Milliseconds(in TimeSpan elapsed) => Math.Round(elapsed.TotalMilliseconds, 1, MidpointRounding.AwayFromZero);

    private static string Path(HttpRequestMessage request) => request.RequestUri?.AbsolutePath ?? "";

    private static string? Header(HttpRequestMessage request, string name) =>
        request.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;

    /// <summary>
    /// A suffix match, not an equality one: the app may be served from a sub-path, which the base address
    /// carries into the resolved URI. The leading slash of the quiet path keeps the match on a segment boundary.
    /// </summary>
    private bool IsQuiet(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.EndsWith(quietPath, StringComparison.OrdinalIgnoreCase) == true;
}

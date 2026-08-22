using EvilBrains.Logging.Contract;

namespace EvilBrains.Logging.WebAssembly;

/// <summary>
/// Stamps every request with identifiers the server puts into its log context.
/// </summary>
internal sealed class RequestContextHandler(IClientIdentity identity) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.RequestId, requestId);
        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.CorrelationId, requestId);
        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.SessionId, identity.SessionId);
        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.MachineId, identity.MachineId);

        return base.SendAsync(request, cancellationToken);
    }
}

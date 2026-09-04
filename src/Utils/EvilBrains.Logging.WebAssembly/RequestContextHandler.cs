using EvilBrains.Logging.Contract;

namespace EvilBrains.Logging.WebAssembly;

internal sealed class RequestContextHandler(IClientIdentity identity) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.RequestId, requestId);
        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.CorrelationId, requestId);
        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.SessionId, identity.SessionId);
        request.Headers.TryAddWithoutValidation(RequestContextHeaderNames.MachineId, identity.MachineId);

        return await base.SendAsync(request, cancellationToken);
    }
}

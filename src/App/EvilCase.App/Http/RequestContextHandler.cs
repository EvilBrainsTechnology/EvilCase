namespace EvilBrains.EvilCase.App.Http;

/// <summary>
/// Stamps every API request with identifiers the server puts into its log context.
/// </summary>
internal sealed class RequestContextHandler(IClientIdentity identity) : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var requestId = Guid.NewGuid().ToString("D", CultureInfo.InvariantCulture);

        request.Headers.TryAddWithoutValidation(ApiRequestHeaderNames.RequestId, requestId);
        request.Headers.TryAddWithoutValidation(ApiRequestHeaderNames.CorrelationId, requestId);
        request.Headers.TryAddWithoutValidation(ApiRequestHeaderNames.SessionId, identity.SessionId);
        request.Headers.TryAddWithoutValidation(ApiRequestHeaderNames.MachineId, identity.MachineId);

        return base.SendAsync(request, cancellationToken);
    }
}

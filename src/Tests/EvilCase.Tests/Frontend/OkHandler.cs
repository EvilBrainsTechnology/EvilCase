using System.Net;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class OkHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request });
    }
}

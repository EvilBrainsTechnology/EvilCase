using System.Net;

namespace EvilBrains.EvilCase.Tests.Frontend;

internal sealed class OkHandler : HttpMessageHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return new HttpResponseMessage(HttpStatusCode.OK) { RequestMessage = request };
    }
}

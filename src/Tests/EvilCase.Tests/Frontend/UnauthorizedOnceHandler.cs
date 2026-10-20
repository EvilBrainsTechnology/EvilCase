using System.Net;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// Answers the first request 401 and every later one 200, keeping what each request carried.
/// </summary>
internal sealed class UnauthorizedOnceHandler : HttpMessageHandler
{
    public List<SentRequest> Sent { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        this.Sent.Add(new() { Options = new Dictionary<string, object?>(request.Options), Body = body });

        var status = this.Sent.Count == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK;

        return new HttpResponseMessage(status) { RequestMessage = request };
    }
}

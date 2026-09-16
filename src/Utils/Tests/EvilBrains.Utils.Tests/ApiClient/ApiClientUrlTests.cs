using EvilBrains.ApiClient;

namespace EvilBrains.Utils.Tests.ApiClient;

public class ApiClientUrlTests
{
    [Test]
    public async Task ACollectionQueryValueIsSentOncePerValueTest()
    {
        string[] labelIds = ["a", "b"];

        var uri = await this.Request([("labelIds", labelIds), ("take", 20)]);

        Assert.That(uri, Is.EqualTo("https://localhost/api/cases?labelIds=a&labelIds=b&take=20"), "a collection reaches the wire as one query pair per value");
    }

    [Test]
    public async Task AnEmptyCollectionAddsNoQueryPairTest()
    {
        var uri = await this.Request([("labelIds", Array.Empty<string>()), ("take", 20)]);

        Assert.That(uri, Is.EqualTo("https://localhost/api/cases?take=20"), "an empty collection adds nothing to the query string");
    }

    [Test]
    public async Task AQueryValueIsEscapedTest()
    {
        string[] labelIds = ["x y"];

        var uri = await this.Request([("search", "a b&c"), ("labelIds", labelIds)]);

        Assert.That(uri, Is.EqualTo("https://localhost/api/cases?search=a%20b%26c&labelIds=x%20y"), "every value is escaped, whether it stands alone or in a collection");
    }

    private async Task<string> Request((string Name, object? Value)[] query)
    {
        var handler = new CapturingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        await ApiClientHttp.Send(client, HttpMethod.Get, "/api/cases", CancellationToken.None, query: query);

        return handler.Uri!.AbsoluteUri;
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public Uri? Uri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            this.Uri = request.RequestUri;

            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.NoContent));
        }
    }
}

using System.Net;
using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Aimed at the anonymous logout, which answers 204 to anyone: any other status is the guard's.
/// </summary>
public class CrossSiteWriteTests
{
    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(rateLimiter: false);

        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [TestCase("cross-site")]
    [TestCase("same-site")]
    [TestCase("none")]
    public async Task AWriteTheBrowserCallsNotSameOriginIsForbidden(string site)
    {
        using var response = await this.Post(secFetchSite: site, origin: null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        }
    }

    [Test]
    public async Task AWriteWithAnotherOriginAndNoFetchMetadataIsForbidden()
    {
        using var response = await this.Post(secFetchSite: null, origin: "https://evil.example");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task AnOriginOfAnotherSchemeOrPortIsForbidden()
    {
        using var scheme = await this.Post(secFetchSite: null, origin: "https://localhost");
        using var port = await this.Post(secFetchSite: null, origin: "http://localhost:81");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(scheme.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
            Assert.That(port.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
        }
    }

    [Test]
    public async Task FetchMetadataDecidesOverTheOrigin()
    {
        using var response = await this.Post(secFetchSite: "cross-site", origin: "http://localhost");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task ASameOriginWriteGoesThrough()
    {
        using var response = await this.Post(secFetchSite: "same-origin", origin: "http://localhost");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task AWriteWithTheOwnOriginAndNoFetchMetadataGoesThrough()
    {
        using var response = await this.Post(secFetchSite: null, origin: "http://localhost");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task AWriteWithNeitherHeaderGoesThrough()
    {
        using var response = await this.Post(secFetchSite: null, origin: null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent), "a client that is not a browser sends neither");
    }

    [Test]
    public async Task AReadIsNeverChecked()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(AuthRoute.Path + "/user-info", UriKind.Relative));
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        using var response = await this.client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized), "authentication answers, not the guard");
    }

    [Test]
    public async Task APathOutsideTheApiIsNotChecked()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri("/some/client/route", UriKind.Relative));
        request.Headers.Add("Sec-Fetch-Site", "cross-site");

        using var response = await this.client.SendAsync(request);

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
    }

    private async Task<HttpResponseMessage> Post(string? secFetchSite, string? origin)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, new Uri(AuthRoute.LogoutPath, UriKind.Relative));

        if (secFetchSite is not null)
            request.Headers.Add("Sec-Fetch-Site", secFetchSite);

        if (origin is not null)
            request.Headers.Add("Origin", origin);

        return await this.client.SendAsync(request);
    }
}

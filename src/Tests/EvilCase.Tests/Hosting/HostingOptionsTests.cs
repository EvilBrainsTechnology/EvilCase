using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The two keys under EvilBrains:EvilCase:Hosting are read as strings and fall back to a default when
/// they do not match, so a typo in either one changes how the deployed application behaves without
/// failing anywhere. Both are pinned through the redirect they decide: with an HTTPS port configured a
/// plain HTTP request is redirected, unless the forwarded scheme says the caller already used HTTPS.
/// </summary>
public class HostingOptionsTests
{
    private const string ForwardedProtoHeader = "X-Forwarded-Proto";

    private static HttpClient CreateClient(EvilCaseHost host) =>
        host.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string? forwardedProto = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/some/client/route", UriKind.Relative));

        if (forwardedProto is not null)
            request.Headers.Add(ForwardedProtoHeader, forwardedProto);

        return await client.SendAsync(request);
    }

    [Test]
    public async Task RedirectionOffLeavesAPlainRequestAlone()
    {
        await using var host = new EvilCaseHost(httpsRedirection: false, httpsPort: 443);
        using var client = CreateClient(host);

        using var response = await GetAsync(client);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task RedirectionOnSendsAPlainRequestToHttps()
    {
        await using var host = new EvilCaseHost(httpsRedirection: true, httpsPort: 443);
        using var client = CreateClient(host);

        using var response = await GetAsync(client);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.TemporaryRedirect));
            Assert.That(response.Headers.Location?.Scheme, Is.EqualTo("https"));
        }
    }

    /// <summary>
    /// The proxy terminates TLS and the request reaches the host over plain HTTP, so without the
    /// forwarded scheme the redirect would bounce a caller that already used HTTPS back to the proxy.
    /// </summary>
    [Test]
    public async Task ForwardedSchemeIsHonouredBehindAReverseProxy()
    {
        await using var host = new EvilCaseHost(behindReverseProxy: true, httpsRedirection: true, httpsPort: 443);
        using var client = CreateClient(host);

        using var response = await GetAsync(client, forwardedProto: "https");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Nothing is known to sit in front, so the header is a claim any caller can make.
    /// </summary>
    [Test]
    public async Task ForwardedSchemeIsIgnoredWithoutAReverseProxy()
    {
        await using var host = new EvilCaseHost(behindReverseProxy: false, httpsRedirection: true, httpsPort: 443);
        using var client = CreateClient(host);

        using var response = await GetAsync(client, forwardedProto: "https");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.TemporaryRedirect));
    }

    /// <summary>
    /// A redirect carries no body, so an orchestrator sending its probes over plain HTTP would count it
    /// as a failed probe and take the instance out of rotation.
    /// </summary>
    [Test]
    public async Task HealthProbesAreNotRedirected()
    {
        await using var host = new EvilCaseHost(httpsRedirection: true, httpsPort: 443);
        using var client = CreateClient(host);

        using var response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}

using System.Net;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.Logging.Contract;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// Authorization is default deny: an endpoint that says nothing needs an authenticated caller. That
/// makes forgetting to protect something harmless and forgetting to open something loud, so the handful
/// of paths that have to stay anonymous are pinned here — the frontend among them, which would
/// otherwise put the sign-in page itself behind a sign-in.
/// </summary>
public class AuthorizationFallbackTests
{
    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: services => services.AddSingleton<ICaseReader>(new StubCaseReader()));
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task AnEndpointThatSaysNothingRequiresAToken()
    {
        using var response = await this.client.GetAsync(new Uri("/api/cases", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task FilingACaseNeedsAToken()
    {
        using var response = await this.client.PostAsJsonAsync(
            new Uri("/api/cases", UriKind.Relative),
            new CreateCaseRequest { Date = new DateOnly(2026, 8, 21), Title = "Spis" });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task HealthProbesStayAnonymous()
    {
        using var live = await this.client.GetAsync(new Uri("/health/live", UriKind.Relative));
        using var ready = await this.client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(live.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(ready.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
        }
    }

    /// <summary>
    /// The frontend uploads its logs from the sign-in page too, where there is nothing to authenticate
    /// with — and a failure there is exactly what one would want to see in the log.
    /// </summary>
    [Test]
    public async Task TheClientLogUploadStaysAnonymous()
    {
        using var response = await this.client.PostAsJsonAsync(
            new Uri(ClientLogRoute.Path, UriKind.Relative),
            new ClientLogBatch { Entries = [] });

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task SigningInAndRenewingStayAnonymous()
    {
        using var login = await this.client.PostAsync(new Uri(AuthRoute.LoginPath, UriKind.Relative), content: null);
        using var refresh = await this.client.PostAsync(new Uri(AuthRoute.RefreshPath, UriKind.Relative), content: null);

        using (Assert.EnterMultipleScope())
        {
            // No body at all, so binding rejects it before the action — which is the point: it got that
            // far, rather than being turned away by the fallback policy.
            Assert.That(login.StatusCode, Is.EqualTo(HttpStatusCode.UnsupportedMediaType));
            Assert.That(refresh.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(refresh.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        }
    }

    [Test]
    public async Task ReadingOnesOwnUserStillNeedsAToken()
    {
        using var response = await this.client.GetAsync(new Uri(AuthRoute.Path + "/user-info", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    [Test]
    public async Task TheFrontendIsServedWithoutASession()
    {
        using var root = await this.client.GetAsync(new Uri("/", UriKind.Relative));
        using var clientRoute = await this.client.GetAsync(new Uri("/cases", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(root.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(clientRoute.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(clientRoute.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
        }
    }

    /// <summary>
    /// An unknown path is a 404 rather than a 401: the fallback policy would otherwise turn every typo
    /// into a claim that the path exists and is protected.
    /// </summary>
    [Test]
    public async Task AnUnknownApiPathIsStillNotFound()
    {
        using var response = await this.client.GetAsync(new Uri("/api/nope", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task ATokenIsEnoughToReachAnOrdinaryEndpoint()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/cases", UriKind.Relative));

        request.Headers.Authorization = TestTokens.BearerFrom(this.host);

        using var response = await this.client.SendAsync(request);

        var body = await response.Content.ReadFromJsonAsync<CaseListResponse>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Items.Select(item => item.Title), Is.EqualTo([StubCaseReader.Title]));
        }
    }
}

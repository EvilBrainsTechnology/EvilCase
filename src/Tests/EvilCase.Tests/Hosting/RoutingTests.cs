using System.Net;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.Cases;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The single host splits the URL space between the API and the frontend on the api/ segment, and the
/// split rests on route precedence alone — the literal segment of the API fallback beating the catch-all
/// serving index.html. Nothing in the type system holds that in place, so it is pinned here.
/// </summary>
public class RoutingTests
{
    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost(configureServices: static services => services.AddSingleton(CaseReaderStub.WithOneCase()));
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task UnknownApiPathIsNotFoundRatherThanTheApp()
    {
        using var response = await this.client.GetAsync(new Uri("/api/nope", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("application/problem+json"));
        }
    }

    [Test]
    public async Task UnknownApiPathAnswersEveryMethodAndDepth()
    {
        using var deep = await this.client.GetAsync(new Uri("/api/one/two/three", UriKind.Relative));
        using var posted = await this.client.PostAsync(new Uri("/api/nope", UriKind.Relative), content: null);
        using var bare = await this.client.GetAsync(new Uri("/api", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deep.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(posted.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
            Assert.That(bare.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
        }
    }

    [Test]
    public async Task ControllerActionIsReachedUnderTheApiPrefix()
    {
        // Authorization is default deny, so an ordinary endpoint needs a token before routing to it
        // proves anything: without one the answer would be 401 whether the route matched or not.
        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri("/api/cases", UriKind.Relative)) { Headers = { Authorization = TestTokens.BearerFrom(this.host) } };

        using var response = await this.client.SendAsync(request);

        var body = await response.Content.ReadFromJsonAsync<CaseListResponse>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body?.Items.Select(static item => item.Title), Is.EqualTo([CaseReaderStub.Title]));
        }
    }

    /// <summary>
    /// A client-side route has to survive a reload, so anything outside the API is the app's entry point.
    /// </summary>
    [Test]
    public async Task ClientSideRouteFallsBackToTheApp()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
        }
    }

    [Test]
    public async Task LivenessProbeAnswersWithoutTouchingDependencies()
    {
        using var response = await this.client.GetAsync(new Uri("/health/live", UriKind.Relative));

        var body = await response.Content.ReadAsStringAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(body, Is.EqualTo("Healthy"));
        }
    }

    /// <summary>
    /// They sit outside the api/ segment, so the API fallback must not swallow them.
    /// </summary>
    [Test]
    public async Task HealthEndpointsAreNotShadowedByEitherFallback()
    {
        using var response = await this.client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.NotFound));
    }

    /// <summary>
    /// Every link of the registration chain forwards to the one below it, and nothing fails to compile
    /// when one of them stops.
    /// </summary>
    [Test]
    public async Task TheReadyProbeRunsTheDatabaseCheck()
    {
        using var response = await this.client.GetAsync(new Uri("/health/ready", UriKind.Relative));

        var body = await response.Content.ReadAsStringAsync();

        Assert.That(body, Does.Contain("\"database\""));
    }
}

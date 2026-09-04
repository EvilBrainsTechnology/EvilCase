using System.Net;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.Logging.Contract;

namespace EvilBrains.EvilCase.Tests.Hosting;

public class RateLimitingTests
{
    private const int PastTheLimit = 200;

    private const int WithinTheLimit = 100;

    private const int AuthPermitLimit = 10;

    private const int LoginPermitLimit = 5;

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost();
        this.client = this.host.CreateClient();
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    [Test]
    public async Task HealthProbesAreNeverLimited()
    {
        var statusCodes = new List<HttpStatusCode>();

        for (var i = 0; i < PastTheLimit; i++)
        {
            using var response = await this.client.GetAsync(new Uri("/health/live", UriKind.Relative));

            statusCodes.Add(response.StatusCode);
        }

        Assert.That(statusCodes, Is.All.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task ClientLogUploadIsLimited()
    {
        var statusCodes = new List<HttpStatusCode>();
        string? retryAfter = null;

        for (var i = 0; i < PastTheLimit; i++)
        {
            using var response = await this.PostBatch();

            statusCodes.Add(response.StatusCode);
            retryAfter ??= response.Headers.RetryAfter?.ToString();
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statusCodes.Take(WithinTheLimit), Is.All.EqualTo(HttpStatusCode.OK));
            Assert.That(statusCodes, Does.Contain(HttpStatusCode.TooManyRequests));
            Assert.That(retryAfter, Is.Not.Null);
        }
    }

    [Test]
    public async Task AuthEndpointsAreLimited()
    {
        var statusCodes = new List<HttpStatusCode>();

        for (var i = 0; i < AuthPermitLimit + 1; i++)
        {
            using var response = await this.client.GetAsync(new Uri("/api/auth/user-info", UriKind.Relative));

            statusCodes.Add(response.StatusCode);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statusCodes.Take(AuthPermitLimit), Is.All.EqualTo(HttpStatusCode.Unauthorized), "the limiter sits ahead of the authentication middleware, so a 401 still spends a permit");
            Assert.That(statusCodes[^1], Is.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    [Test]
    public async Task SignInIsLimitedSeparatelyAndSooner()
    {
        var statusCodes = new List<HttpStatusCode>();

        for (var i = 0; i < LoginPermitLimit + 1; i++)
        {
            using var response = await this.client.PostAsync(new Uri(AuthRoute.LoginPath, UriKind.Relative), content: null);

            statusCodes.Add(response.StatusCode);
        }

        using var afterwards = await this.client.PostAsync(new Uri(AuthRoute.RefreshPath, UriKind.Relative), content: null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(statusCodes.Take(LoginPermitLimit), Is.All.EqualTo(HttpStatusCode.UnsupportedMediaType));
            Assert.That(statusCodes[^1], Is.EqualTo(HttpStatusCode.TooManyRequests));
            Assert.That(afterwards.StatusCode, Is.Not.EqualTo(HttpStatusCode.TooManyRequests), "sign-in has a partition of its own, and this bodyless call reaches binding rather than the limiter");
        }
    }

    private async Task<HttpResponseMessage> PostBatch()
    {
        return await this.client.PostAsJsonAsync(
            new Uri(ClientLogRoute.Path, UriKind.Relative),
            new ClientLogBatch { Entries = [] });
    }
}

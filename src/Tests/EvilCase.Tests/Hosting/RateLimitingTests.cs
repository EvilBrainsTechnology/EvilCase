using System.Net;
using System.Net.Http.Json;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.Logging.Contract;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The anonymous endpoints are the ones a single caller can make expensive, and the log upload is the one
/// whose successful requests are not even logged. Everything else, the health probes above all, has to
/// stay unlimited: a limiter reaching them would take instances out of rotation under load.
/// </summary>
public class RateLimitingTests
{
    /// <summary>
    /// Comfortably past the permit limit the host configures for the upload path.
    /// </summary>
    private const int PastTheLimit = 200;

    /// <summary>
    /// Below it, so nothing up to here may be rejected.
    /// </summary>
    private const int WithinTheLimit = 100;

    /// <summary>
    /// The permit limit the host configures for the auth path, which is low enough to be spent exactly.
    /// </summary>
    private const int AuthPermitLimit = 10;

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
            using var response = await this.PostBatchAsync();

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

    /// <summary>
    /// The limiter sits ahead of the authentication middleware, so a rejected caller pays for its attempts
    /// too — which is the point, the expensive one is login and it is anonymous.
    /// </summary>
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
            Assert.That(statusCodes.Take(AuthPermitLimit), Is.All.EqualTo(HttpStatusCode.Unauthorized));
            Assert.That(statusCodes[^1], Is.EqualTo(HttpStatusCode.TooManyRequests));
        }
    }

    private Task<HttpResponseMessage> PostBatchAsync() =>
        this.client.PostAsJsonAsync(
            new Uri(ClientLogRoute.Path, UriKind.Relative),
            new ClientLogBatch { Entries = [] });
}

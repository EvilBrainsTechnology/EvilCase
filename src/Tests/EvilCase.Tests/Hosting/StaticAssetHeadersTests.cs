using System.Net;
using System.Net.Http.Headers;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The frontend is served from the build's endpoint manifest, which answers every static file
/// compressed. The immutable cache the manifest also carries stays out of reach here: a host reading
/// static web assets from the source tree rewrites every Cache-Control to no-cache, and the test host
/// does exactly that.
/// </summary>
public class StaticAssetHeadersTests
{
    private const string RuntimePrefix = "dotnet.native.";

    private const string RuntimeSuffix = ".wasm";

    private EvilCaseHost host = null!;

    private HttpClient client = null!;

    [OneTimeSetUp]
    public void SetUp()
    {
        this.host = new EvilCaseHost();
        this.client = this.host.CreateClient();
        this.client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
    }

    [OneTimeTearDown]
    public void TearDown()
    {
        this.client.Dispose();
        this.host.Dispose();
    }

    /// <summary>
    /// The runtime is the largest download of the boot, and the only middleware that used to serve it is
    /// gone.
    /// </summary>
    [Test]
    public async Task TheRuntimeIsServedCompressed()
    {
        var runtime = await this.FingerprintedRuntime();

        using var response = await this.client.GetAsync(new Uri($"/_framework/{runtime}", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content.Headers.ContentEncoding, Does.Contain("gzip"));
        }
    }

    /// <summary>
    /// The entry point of the frontend, which the fallback would answer uncompressed.
    /// </summary>
    [Test]
    public async Task TheEntryPointIsServedCompressed()
    {
        using var response = await this.client.GetAsync(new Uri("/", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Content.Headers.ContentType?.MediaType, Is.EqualTo("text/html"));
            Assert.That(response.Content.Headers.ContentEncoding, Does.Contain("gzip"));
        }
    }

    /// <summary>
    /// The stylesheets block the first render and are the bulk of what is downloaded before the runtime.
    /// </summary>
    [Test]
    public async Task AStylesheetIsServedCompressed()
    {
        using var response = await this.client.GetAsync(new Uri("/lib/tabler/tabler.min.css", UriKind.Relative));

        Assert.That(response.Content.Headers.ContentEncoding, Does.Contain("gzip"));
    }

    /// <summary>
    /// The boot config is inlined into dotnet.js. It names the runtime twice: once plain, once under the
    /// fingerprint it is actually downloaded by.
    /// </summary>
    private async Task<string> FingerprintedRuntime()
    {
        var bootScript = await this.client.GetStringAsync(new Uri("/_framework/dotnet.js", UriKind.Relative));

        for (var start = 0; (start = bootScript.IndexOf(RuntimePrefix, start, StringComparison.Ordinal)) >= 0; start++)
        {
            var fingerprint = bootScript[(start + RuntimePrefix.Length)..];
            var end = fingerprint.IndexOf(RuntimeSuffix, StringComparison.Ordinal);
            if (end > 0 && fingerprint[..end].All(char.IsAsciiLetterOrDigit))
                return RuntimePrefix + fingerprint[..(end + RuntimeSuffix.Length)];
        }

        Assert.Fail("the boot config names no fingerprinted runtime file");

        return "";
    }
}

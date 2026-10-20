namespace EvilBrains.EvilCase.Tests.Hosting;

public class SecurityHeadersTests
{
    private const string ScriptOpenTag = "<script>";

    private const string ScriptCloseTag = "</script>";

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
    public async Task EveryResponseCarriesTheBaselineHeaders()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Header(response, "Content-Security-Policy"), Does.Contain("frame-ancestors 'none'"));
            Assert.That(Header(response, "X-Content-Type-Options"), Is.EqualTo("nosniff"));
            Assert.That(Header(response, "Referrer-Policy"), Is.EqualTo("no-referrer"));
            Assert.That(Header(response, "X-Frame-Options"), Is.EqualTo("DENY"));
            Assert.That(
                Header(response, "Permissions-Policy"),
                Is.EqualTo("camera=(), microphone=(), geolocation=()"),
                "every response must deny camera, microphone and geolocation");
        }
    }

    [Test]
    public async Task PolicyAllowsTheWebAssemblyRuntime()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        Assert.That(Header(response, "Content-Security-Policy"), Does.Contain("'wasm-unsafe-eval'"), "Blazor WebAssembly compiles its runtime, which no policy without this source expression allows");
    }

    [Test]
    public async Task ThePreviewMayShowBlobUrlsInImagesOnlyAndRunItsPdfWorkerFromTheApp()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        var directives = Header(response, "Content-Security-Policy")!.Split("; ");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(directives, Does.Contain("img-src 'self' blob:"), "a converted picture is a blob URL");
            Assert.That(directives, Does.Contain("media-src 'self'"), "audio and video load from the content address");
            Assert.That(directives, Does.Contain("worker-src 'self'"), "the PDF library runs its worker from the app");
            Assert.That(directives.Where(static directive => directive.StartsWith("frame-src", StringComparison.Ordinal)), Is.Empty, "nothing is framed");
            Assert.That(directives.Where(static directive => directive.Contains("blob:", StringComparison.Ordinal)), Has.Exactly(1).Items, "no other directive allows blob:");
            Assert.That(directives, Does.Contain("object-src 'none'"));
            Assert.That(directives, Does.Contain("frame-ancestors 'none'"), "the app is framed by nobody");
            Assert.That(Header(response, "X-Frame-Options"), Is.EqualTo("DENY"));
            Assert.That(directives, Does.Contain("script-src 'self' 'wasm-unsafe-eval'"), "a blob URL never runs script");
            Assert.That(directives, Does.Contain("connect-src 'self'"));
            Assert.That(string.Join(' ', directives), Does.Not.Contain("'unsafe-eval'"), "no script is built from a string");
        }
    }

    [Test]
    public async Task TheAppCarriesNoInlineScriptAndThePolicyNoHash()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        var html = await response.Content.ReadAsStringAsync();
        var policy = Header(response, "Content-Security-Policy");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(InlineScripts(html), Is.Empty, "an inline script the policy carries no hash for is blocked in the browser");
            Assert.That(policy, Does.Not.Contain("'sha256-"), "the policy allows a script no page carries");
        }
    }

    private static string? Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    /// <summary>
    /// Scripts with a source carry attributes, so the bare opening tag is what separates the two kinds.
    /// </summary>
    private static List<string> InlineScripts(string html)
    {
        var scripts = new List<string>();
        var index = 0;

        while (true)
        {
            var open = html.IndexOf(ScriptOpenTag, index, StringComparison.Ordinal);
            if (open < 0)
                return scripts;

            var start = open + ScriptOpenTag.Length;
            var close = html.IndexOf(ScriptCloseTag, start, StringComparison.Ordinal);
            if (close < 0)
                return scripts;

            scripts.Add(html[start..close]);
            index = close + ScriptCloseTag.Length;
        }
    }
}

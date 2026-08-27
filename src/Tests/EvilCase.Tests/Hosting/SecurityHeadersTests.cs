using System.Security.Cryptography;
using System.Text;

namespace EvilBrains.EvilCase.Tests.Hosting;

/// <summary>
/// The content security policy is written once, in the host, while what it has to allow lives in the
/// frontend: a script hash that no longer matches index.html only shows up as a blank page in a browser.
/// </summary>
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

    /// <summary>
    /// Blazor WebAssembly compiles its runtime, which no policy without this source expression allows.
    /// </summary>
    [Test]
    public async Task PolicyAllowsTheWebAssemblyRuntime()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        Assert.That(Header(response, "Content-Security-Policy"), Does.Contain("'wasm-unsafe-eval'"));
    }

    /// <summary>
    /// index.html boots the theme from an inline script, which a policy without its hash would block.
    /// </summary>
    [Test]
    public async Task PolicyCarriesTheHashOfEveryInlineScriptOfTheApp()
    {
        using var response = await this.client.GetAsync(new Uri("/some/client/route", UriKind.Relative));

        var html = await response.Content.ReadAsStringAsync();
        var policy = Header(response, "Content-Security-Policy");
        var scripts = InlineScripts(html);

        Assert.That(scripts, Is.Not.Empty);

        using (Assert.EnterMultipleScope())
        {
            foreach (var script in scripts)
                Assert.That(policy, Does.Contain(Hash(script)));
        }
    }

    private static string? Header(HttpResponseMessage response, string name)
    {
        return response.Headers.TryGetValues(name, out var values) ? values.FirstOrDefault() : null;
    }

    private static string Hash(string script)
    {
        return "'sha256-" + Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(script))) + "'";
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

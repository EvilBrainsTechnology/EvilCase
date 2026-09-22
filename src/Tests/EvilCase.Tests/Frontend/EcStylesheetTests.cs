using System.Net;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcStylesheetTests
{
    private static readonly string[] CheckedProperties =
    [
        "color",
        "background",
        "background-color",
        "border-color",
        "font-family",
        "font-size",
        "border-radius",
        "height",
        "min-height",
        "width",
        "min-width",
        "gap",
        "padding",
    ];

    private static readonly string[] AllowedLiterals = ["0", "auto", "none", "transparent", "inherit", "currentcolor"];

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
    public async Task TheAppRootOpensTheTokenScope()
    {
        var html = await this.client.GetStringAsync(new Uri("/", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(html, Does.Contain("<div id=\"app\" class=\"ec\">"), "without the ec scope no component reaches a token");
            Assert.That(html, Does.Contain("css/ec-tokens.css"), "without the ec scope no component reaches a token");
            Assert.That(html, Does.Contain("css/ec-fonts.css"), "without the ec scope no component reaches a token");
            Assert.That(html, Does.Contain("css/ec-components.css"), "without the ec scope no component reaches a token");
        }
    }

    [Test]
    public async Task TheBootScreenIsTheSameBrandAsTheSignInPage()
    {
        var html = await this.client.GetStringAsync(new Uri("/", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(html, Does.Contain("class=\"ec-boot\""));
            Assert.That(html, Does.Contain("class=\"ec-brand\""));
            Assert.That(html, Does.Contain("class=\"ec-brand-mark\""));
            Assert.That(html, Does.Not.Contain("page-center"), "the boot screen carries no Tabler class (SDD-020)");
            Assert.That(html, Does.Not.Contain("container-tight"), "the boot screen carries no Tabler class (SDD-020)");
            Assert.That(html, Does.Not.Contain("progress-bar"), "the boot screen carries no Tabler class (SDD-020)");
        }
    }

    [Test]
    public async Task TheFontsComeFromTheAppAndNotFromAForeignNetwork()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-fonts.css", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css, Does.Contain("@font-face"));
            Assert.That(css, Does.Contain("Instrument Sans"));
            Assert.That(css, Does.Contain("JetBrains Mono"));
            Assert.That(css, Does.Not.Contain("http"));
        }

        foreach (var url in ExtractUrls(css))
        {
            var path = url.StartsWith("../", StringComparison.Ordinal) ? url["..".Length..] : url;

            using var response = await this.client.GetAsync(new Uri(path, UriKind.Relative));
            var body = await response.Content.ReadAsByteArrayAsync();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(body, Has.Length.GreaterThan(1000));
            }
        }
    }

    [Test]
    public async Task TheOldSheetDefinesNoTokenNameTheNewOneDefines()
    {
        var appCss = await this.client.GetStringAsync(new Uri("/css/app.css", UriKind.Relative));
        var tokensCss = await this.client.GetStringAsync(new Uri("/css/ec-tokens.css", UriKind.Relative));

        var shared = DefinedNames(appCss)
            .Intersect(DefinedNames(tokensCss), StringComparer.Ordinal)
            .ToArray();

        Assert.That(
            shared,
            Is.Empty,
            "the ec scope on the app root shadows a name the old sheet defines on :root, so an unmigrated screen silently takes the new value");
    }

    [Test]
    public async Task TheIconDrawingIsOneRuleForEveryIcon()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css, Does.Contain("stroke-width: 1.8"));
            Assert.That(css, Does.Contain("stroke-linecap: round"));
            Assert.That(css, Does.Contain("stroke-linejoin: round"));
        }
    }

    [Test]
    public async Task NoPrimitiveNamesAColourDirectly()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css, Does.Not.Contain("#"), "a component reads colour from a token");
            Assert.That(css, Does.Not.Contain("rgb("), "a component reads colour from a token");
            Assert.That(css, Does.Not.Contain("rgba("), "a component reads colour from a token");
            Assert.That(css, Does.Not.Contain("hsl("), "a component reads colour from a token");
        }
    }

    [Test]
    public async Task EveryValueOfAPrimitiveComesFromAToken()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            foreach (var rawLine in css.Split('\n'))
            {
                var line = rawLine.Trim();
                if (line.EndsWith(';'))
                    line = line[..^1];

                var colon = line.IndexOf(':', StringComparison.Ordinal);
                if (colon < 0)
                    continue;

                var property = line[..colon].Trim();
                var value = line[(colon + 1)..].Trim();

                if (!CheckedProperties.Contains(property, StringComparer.Ordinal))
                    continue;

                var isToken = value.Contains("var(--ec-", StringComparison.Ordinal)
                    || AllowedLiterals.Contains(value, StringComparer.OrdinalIgnoreCase);

                Assert.That(isToken, Is.True, $"{property}: {value} is not a token");
            }
        }
    }

    private static IEnumerable<string> DefinedNames(string css)
    {
        return css
            .Split('\n')
            .Select(static line => line.Trim())
            .Where(static line => line.StartsWith("--", StringComparison.Ordinal))
            .Where(static line => line.Contains(':', StringComparison.Ordinal))
            .Select(static line => line[..line.IndexOf(':', StringComparison.Ordinal)].Trim());
    }

    private static IEnumerable<string> ExtractUrls(string css)
    {
        var urls = new List<string>();

        for (var start = css.IndexOf("url('", StringComparison.Ordinal); start >= 0; start = css.IndexOf("url('", start, StringComparison.Ordinal))
        {
            start += "url('".Length;
            var end = css.IndexOf('\'', start);
            urls.Add(css[start..end]);
            start = end;
        }

        return urls;
    }
}

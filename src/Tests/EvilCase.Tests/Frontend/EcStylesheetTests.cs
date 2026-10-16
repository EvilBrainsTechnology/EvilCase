using System.Net;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Domain.Labels;
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
    public async Task TheBootScreenIsTheSameFrameAsTheSignInPage()
    {
        var html = await this.client.GetStringAsync(new Uri("/", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(html, Does.Contain("class=\"ec-gate\""));
            Assert.That(html, Does.Contain("class=\"ec-gate-card\""));
            Assert.That(html, Does.Contain("class=\"ec-gate-facts\""));
            Assert.That(html, Does.Contain("class=\"ec-brand ec-gate-brand\""));
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
    public async Task TheSelectChevronIsAnImageOfTheApp()
    {
        var tokens = await this.client.GetStringAsync(new Uri("/css/ec-tokens.css", UriKind.Relative));

        using var response = await this.client.GetAsync(new Uri("/img/select-chevron.svg", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tokens, Does.Contain("--ec-select-chevron: url(\"../img/select-chevron.svg\");"));
            Assert.That(tokens, Does.Not.Contain("data:"), "the content security policy blocks a data: image");
            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }
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

    [Test]
    public async Task EveryLabelColourPaintsASoftChipWithStrongTextAndDot()
    {
        var components = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));
        var tokens = await this.client.GetStringAsync(new Uri("/css/ec-tokens.css", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            foreach (var color in Enum.GetValues<LabelColor>().Select(LabelColorDisplay.Css))
            {
                Assert.That(tokens, Does.Contain($"--ec-label-{color}-soft:"), $"{color}: the chip background is its soft token");
                Assert.That(tokens, Does.Contain($"--ec-label-{color}-strong:"), $"{color}: the chip text and dot are its strong token");
                Assert.That(components, Does.Contain($"--ec-chip-soft: var(--ec-label-{color}-soft);"), $"{color}: ec-chip-{color} must set the soft token");
                Assert.That(components, Does.Contain($"--ec-chip-strong: var(--ec-label-{color}-strong);"), $"{color}: ec-chip-{color} must set the strong token");
            }
        }
    }

    [Test]
    public async Task EveryListRowIsOneHeight()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css, Does.Not.Contain("--ec-list-row-height:"), "no list sets a row height of its own");

            foreach (var selector in new[] { ".ec-table-row", ".ec-settings-row", ".ec-child-row" })
            {
                var rules = css.Split($"\n{selector} {{").Skip(1).Select(static rest => rest[..rest.IndexOf('}', StringComparison.Ordinal)]);

                Assert.That(rules, Has.Some.Contains("min-height: var(--ec-list-row-height);"), $"{selector} takes the one list row height");
            }
        }
    }

    [Test]
    public async Task TheFormActionsStayAtTheBottomOnMobile()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));

        var mobile = css[css.IndexOf("@media (max-width: 991.98px)", StringComparison.Ordinal)..];
        var rule = mobile[mobile.IndexOf(".ec-form-layout .ec-card-footer {", StringComparison.Ordinal)..];
        rule = rule[..rule.IndexOf('}', StringComparison.Ordinal)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rule, Does.Contain("position: sticky;"));
            Assert.That(rule, Does.Contain("bottom: 0;"));
            Assert.That(rule, Does.Contain("env(safe-area-inset-bottom)"), "a fixed bottom element clears the safe area");
        }
    }

    [Test]
    public async Task AnErrorTakesThePlaceOfTheHint()
    {
        var css = await this.client.GetStringAsync(new Uri("/css/ec-components.css", UriKind.Relative));

        var rule = css[css.IndexOf(".ec-field:has(.ec-field-error > *) .ec-field-hint {", StringComparison.Ordinal)..];
        rule = rule[..rule.IndexOf('}', StringComparison.Ordinal)];

        Assert.That(rule, Does.Contain("display: none;"), "a field shows its error right under the control");
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

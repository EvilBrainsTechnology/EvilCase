using System.Net;
using System.Text.RegularExpressions;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public partial class EcStylesheetTests
{
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

    [TestCase("ec-tokens.css")]
    [TestCase("ec-fonts.css")]
    [TestCase("ec-components.css")]
    public async Task AStylesheetLoadsEveryResourceFromTheApp(string stylesheet)
    {
        var css = await this.client.GetStringAsync(new Uri("/css/" + stylesheet, UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css, Does.Not.Contain("data:"), "the content security policy blocks a data: URL");
            Assert.That(css, Does.Not.Contain("http"), "a stylesheet loads nothing from a foreign network");
        }

        foreach (var url in UrlPattern.Matches(css).Select(static match => match.Groups["url"].Value))
        {
            using var response = await this.client.GetAsync(new Uri(new Uri(this.client.BaseAddress!, "css/"), url));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{url} is served by the app");
        }
    }

    [GeneratedRegex("""url\(['"](?<url>[^'"]+)['"]\)""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlPattern { get; }
}

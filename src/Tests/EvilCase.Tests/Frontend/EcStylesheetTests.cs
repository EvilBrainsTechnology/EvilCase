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

    [Test]
    public async Task AStylesheetLoadsEveryResourceFromTheApp()
    {
        var page = await this.client.GetStringAsync(new Uri("/", UriKind.Relative));
        var stylesheets = StylesheetPattern.Matches(page).Select(static match => match.Groups["href"].Value).ToList();

        Assert.That(stylesheets, Is.Not.Empty, "the entry point links its stylesheets");

        foreach (var stylesheet in stylesheets)
            await this.AssertLoadsFromTheApp(stylesheet);
    }

    private async Task AssertLoadsFromTheApp(string stylesheet)
    {
        var stylesheetUri = new Uri(this.client.BaseAddress!, stylesheet);
        var css = await this.client.GetStringAsync(stylesheetUri);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(css, Does.Not.Contain("data:"), "the content security policy blocks a data: URL");
            Assert.That(css, Does.Not.Contain("http"), "a stylesheet loads nothing from a foreign network");
        }

        var urls = UrlPattern.Matches(css).Select(static match => match.Groups["url"].Value).ToList();

        Assert.That(urls, Is.Not.Empty, $"{stylesheet} loads the app's fonts");

        foreach (var url in urls)
        {
            using var response = await this.client.GetAsync(new Uri(stylesheetUri, url));

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK), $"{url} is served by the app");
        }
    }

    [GeneratedRegex("""<link rel="stylesheet" href="(?<href>[^"]+)" />""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex StylesheetPattern { get; }

    [GeneratedRegex("""url\((?<quote>['"]?)(?<url>[^'")]+)\k<quote>\)""", RegexOptions.None, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlPattern { get; }
}

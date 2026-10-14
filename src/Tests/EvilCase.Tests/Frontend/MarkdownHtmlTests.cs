using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class MarkdownHtmlTests
{
    [Test]
    public void MarkdownBecomesFormattedHtml()
    {
        var html = MarkdownHtml.Render("# Nadpis\n\nText s **tučným** a `kódem`.\n\n- jedna\n- dvě\n\n| a | b |\n|---|---|\n| 1 | 2 |\n");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(html, Does.Contain("<h1>Nadpis</h1>"));
            Assert.That(html, Does.Contain("<strong>tučným</strong>"));
            Assert.That(html, Does.Contain("<code>kódem</code>"));
            Assert.That(html, Does.Contain("<li>dvě</li>"));
            Assert.That(html, Does.Contain("<td>2</td>"));
        }
    }

    [TestCase("<script>alert(1)</script>")]
    [TestCase("<img src=x onerror=alert(1)>")]
    [TestCase("<a href=\"javascript:alert(1)\" onclick=\"alert(1)\">x</a>")]
    [TestCase("<div style=\"x\">blok</div>")]
    [TestCase("<iframe src=\"https://example.org\"></iframe>")]
    public void RawHtmlShowsAsTextAndNeverAsAnElement(string markdown)
    {
        var html = MarkdownHtml.Render(markdown);

        Assert.That(html, Does.Not.Contain("<script").And.Not.Contain("<img").And.Not.Contain("<a ").And.Not.Contain("<div").And.Not.Contain("<iframe"));
    }

    [TestCase("[x](javascript:alert(1))")]
    [TestCase("[x](JavaScript:alert(1))")]
    [TestCase("[x](&#106;avascript:alert(1))")]
    [TestCase("[x](data:text/html;base64,PHNjcmlwdD4=)")]
    [TestCase("[x](vbscript:msgbox(1))")]
    [TestCase("[x](/relativni/cesta)")]
    [TestCase("[x][r]\n\n[r]: javascript:alert(1)")]
    [TestCase("<javascript:alert(1)>")]
    public void OnlyAWebOrMailAddressStaysALink(string markdown)
    {
        var html = MarkdownHtml.Render(markdown);

        Assert.That(html, Does.Not.Contain("href"));
    }

    [TestCase("[x](https://example.org/a?b=1)", "https://example.org/a?b=1")]
    [TestCase("[x](http://example.org)", "http://example.org")]
    [TestCase("[x](mailto:jana@example.org)", "mailto:jana@example.org")]
    [TestCase("<https://example.org/auto>", "https://example.org/auto")]
    [TestCase("https://example.org/bare", "https://example.org/bare")]
    public void ALinkOpensInANewTabWithoutAccessToThePage(string markdown, string address)
    {
        var html = MarkdownHtml.Render(markdown);

        Assert.That(html, Does.Contain($"<a href=\"{address}\" target=\"_blank\" rel=\"noopener noreferrer\">"));
    }

    [Test]
    public void AnImageIsNeverLoadedAndItsDescriptionShowsInstead()
    {
        var html = MarkdownHtml.Render("![popis obrázku](https://example.org/x.png)");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(html, Does.Not.Contain("<img"));
            Assert.That(html, Does.Contain("popis obrázku"));
        }
    }
}

using System.Net;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class MarkdownEditorLibraryTests
{
    [Test]
    public async Task TheMarkdownLibraryIsServedWithItsStylesheetAndTheLicensesOfWhatItBundles()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        using var library = await client.GetAsync(new Uri("/lib/milkdown/milkdown.min.js", UriKind.Relative));
        using var stylesheet = await client.GetAsync(new Uri("/lib/milkdown/prosemirror.css", UriKind.Relative));
        var licenses = await client.GetStringAsync(new Uri("/lib/milkdown/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the Markdown library is served");
            Assert.That(stylesheet.StatusCode, Is.EqualTo(HttpStatusCode.OK), "the Markdown library's stylesheet is served");
            Assert.That(licenses, Does.Contain("===== @milkdown/core "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Contain("===== @milkdown/prose "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Contain("===== prosemirror-view "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Contain("===== remark-gfm "), "the licenses name what the bundle holds");
            Assert.That(licenses, Does.Not.Contain("===== vue "), "the bundle leaves out Milkdown's components");
            Assert.That(licenses, Does.Not.Contain("===== dompurify "), "the bundle leaves out Milkdown's components");
        }
    }

    [Test]
    public async Task TheMarkdownLibraryLinksOnlyToWebAndMailAddressesInANewTab()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        var library = await client.GetStringAsync(new Uri("/lib/milkdown/milkdown.min.js", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library, Does.Contain("(https?:|mailto:)"), "a link stays a link only to a web or e-mail address");
            Assert.That(library, Does.Contain("noopener noreferrer"), "a link opens in a new tab without the opener");
        }
    }

    [Test]
    public async Task TheEditorLibraryHighlightsMarkdown()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        var licenses = await client.GetStringAsync(new Uri("/lib/codemirror/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(licenses, Does.Contain("===== @codemirror/lang-markdown "), "the source editor highlights Markdown");
            Assert.That(licenses, Does.Contain("===== @lezer/markdown "), "the source editor highlights Markdown");
        }
    }
}

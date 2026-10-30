using System.Net;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class TextEditorLibraryTests
{
    [Test]
    public async Task TheEditorLibraryIsServedWithTheLicensesOfWhatItBundles()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        using var library = await client.GetAsync(new Uri("/lib/codemirror/codemirror.min.js", UriKind.Relative));
        var licenses = await client.GetStringAsync(new Uri("/lib/codemirror/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(library.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(licenses, Does.Contain("===== @codemirror/view "));
            Assert.That(licenses, Does.Contain("===== @lezer/highlight "));
        }
    }
}

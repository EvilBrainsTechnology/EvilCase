using System.Net;
using System.Text.Json;
using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class TextEditorLibraryTests
{
    // Prints the summary of the compare of the two texts.
    private const string Summary = """
        const { compareSummary } = await import(process.argv[1]);
        const [older, newer] = JSON.parse(process.argv[2]);
        const { added, removed, changes } = compareSummary(older, newer);
        console.log(JSON.stringify([added, removed, changes]));
        """;

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
            Assert.That(licenses, Does.Contain("===== @codemirror/merge "));
        }
    }

    [Test]
    public async Task TheCompareCountsTheChangedLinesOfEachSideAndIgnoresLineBreaks()
    {
        using var library = new ServedLibrary("/lib/codemirror/codemirror.min.js");

        int[] changed = [3, 2, 2];
        int[] none = [0, 0, 0];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await Compare(library, "a\nb\nc\nd\n", "a\nB\nc\nx\ny\n"), Is.EqualTo(changed), "a changed line counts on both sides");
            Assert.That(await Compare(library, "a\nb\n", "a\r\nb\r\n"), Is.EqualTo(none), "a difference in line breaks alone is none");
        }
    }

    private static async Task<int[]> Compare(ServedLibrary library, string older, string newer)
    {
        return JsonSerializer.Deserialize<int[]>(await library.Run(Summary, JsonSerializer.Serialize<string[]>([older, newer])))!;
    }
}

using EvilBrains.EvilCase.Tests.Hosting;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FloatingUiLibraryTests
{
    // Prints the names of the functions the library exports, sorted, one per line.
    private const string Exports = """
        const library = await import(process.argv[1]);
        console.log(Object.keys(library).filter((name) => typeof library[name] === 'function').sort().join('\n'));
        """;

    [Test]
    public async Task TheLibraryExportsWhatTheTooltipImports()
    {
        using var library = new ServedLibrary("/lib/floating-ui/floating-ui.min.js");

        var output = await library.Run(Exports);

        Assert.That(
            output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            Is.EqualTo(["arrow", "autoUpdate", "computePosition", "flip", "hide", "inline", "offset", "shift"]));
    }

    [Test]
    public async Task TheLicensesNameEveryBundledPackage()
    {
        await using var host = new EvilCaseHost();
        using var client = host.CreateClient();

        var licenses = await client.GetStringAsync(new Uri("/lib/floating-ui/LICENSE.txt", UriKind.Relative));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(licenses, Does.Contain("===== @floating-ui/core "));
            Assert.That(licenses, Does.Contain("===== @floating-ui/dom "));
            Assert.That(licenses, Does.Contain("===== @floating-ui/utils "));
        }
    }
}

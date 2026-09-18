using Bunit;
using EvilBrains.EvilCase.App.Components.Ec;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class EcFileChipRenderTests
{
    [Test]
    public void TheChipShowsTheExtensionUppercase()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcFileChip>(static parameters => parameters
            .Add(static chip => chip.FileName, "Rozhodnutí o přestupku.PDF"));

        Assert.That(component.Find("span.ec-file-chip").TextContent.Trim(), Is.EqualTo("PDF"));
    }

    [Test]
    public void TheToneFollowsTheFileType()
    {
        (string FileName, string Tone)[] cases =
        [
            ("a.pdf", "ec-file-chip-pdf"),
            ("a.docx", "ec-file-chip-document"),
            ("a.csv", "ec-file-chip-sheet"),
            ("a.png", "ec-file-chip-image"),
            ("a.zip", "ec-file-chip-other"),
            ("README", "ec-file-chip-other"),
        ];

        foreach (var (fileName, tone) in cases)
        {
            using var ctx = new BunitContext();

            var component = ctx.Render<EcFileChip>(parameters => parameters
                .Add(static chip => chip.FileName, fileName));

            Assert.That(
                component.Find("span.ec-file-chip").ClassList,
                Is.EquivalentTo(["ec-file-chip", tone]),
                "the chip takes its colour from the file type");
        }
    }

    [Test]
    public void TheChipCarriesNoForeignClass()
    {
        using var ctx = new BunitContext();

        var component = ctx.Render<EcFileChip>(static parameters => parameters
            .Add(static chip => chip.FileName, "a.pdf"));

        Assert.That(
            component.FindAll("*").All(static element => element.ClassList.All(static name => name.StartsWith("ec-", StringComparison.Ordinal))),
            Is.True,
            "a new component carries no bootstrap or Tabler class");
    }
}

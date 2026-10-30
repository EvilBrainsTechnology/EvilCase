using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileTypeDisplayTests
{
    [TestCase("Smlouva o dílo.pdf.p7m", "Smlouva o dílo.pdf")]
    [TestCase("Podpis.P7S", "Podpis")]
    public void TheSignedContentDropsTheSignatureSuffix(string fileName, string contentName)
    {
        Assert.That(FileTypeDisplay.SignedContentName(fileName), Is.EqualTo(contentName));
    }

    [TestCase("Poznámky.txt", FileTypeDisplay.MaxTextBytes, ExpectedResult = true)]
    [TestCase("data.JSON", 10, ExpectedResult = true)]
    [TestCase("README.md", 10, ExpectedResult = true)]
    [TestCase("Poznámky.txt", FileTypeDisplay.MaxTextBytes + 1, ExpectedResult = false)]
    [TestCase("Smlouva.pdf", 10, ExpectedResult = false)]
    [TestCase("Smlouva.xml.p7m", 10, ExpectedResult = false)]
    [TestCase("bez přípony", 10, ExpectedResult = false)]
    public bool OnlyTextCodeAndMarkdownWithinTheReadLimitAreEditable(string fileName, long sizeBytes)
    {
        return FileTypeDisplay.Editable(fileName, sizeBytes);
    }
}

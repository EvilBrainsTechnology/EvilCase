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
}

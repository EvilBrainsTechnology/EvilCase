using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileExtensionDisplayTests
{
    [Test]
    public void ShortExtensionReadsUppercase()
    {
        Assert.That(FileExtensionDisplay.Text("odpor-proti-prikazu.pdf"), Is.EqualTo("PDF"));
    }

    [Test]
    public void LongExtensionTruncatesToFourCharacters()
    {
        Assert.That(FileExtensionDisplay.Text("podklady.torrent"), Is.EqualTo("TORR"));
    }

    [Test]
    public void NoExtensionReadsAsFile()
    {
        Assert.That(FileExtensionDisplay.Text("README"), Is.EqualTo("FILE"));
    }
}

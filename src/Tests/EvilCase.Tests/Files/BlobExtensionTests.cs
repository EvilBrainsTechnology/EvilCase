using EvilBrains.EvilCase.Files;

namespace EvilBrains.EvilCase.Tests.Files;

public class BlobExtensionTests
{
    [TestCase("protokol.pdf", ".pdf")]
    [TestCase("PROTOKOL.PDF", ".PDF")]
    [TestCase("archive.tar.gz", ".gz")]
    [TestCase(".env", ".env")]
    [TestCase("C:\\temp\\scan.jpeg", ".jpeg")]
    public void KeepsTheExtension(string fileName, string expected)
    {
        Assert.That(BlobExtension.From(fileName), Is.EqualTo(expected), "a plain ASCII extension must survive unchanged");
    }

    [TestCase(null, "")]
    [TestCase("", "")]
    [TestCase("protokol", "")]
    [TestCase("protokol.", "")]
    [TestCase("protokol.p df", "")]
    [TestCase("protokol.pdf ", "")]
    [TestCase("protokol.jedenznaku1", "")]
    [TestCase("protokol.pdfč", "")]
    [TestCase("protokol.p-df", "")]
    [TestCase("scan.jpg:zone", "")]
    [TestCase(".", "")]
    [TestCase("..", "")]
    public void DropsWhatIsNotAnExtension(string? fileName, string expected)
    {
        Assert.That(BlobExtension.From(fileName), Is.EqualTo(expected), "nothing but a whitelisted extension may reach the blob path");
    }

    [TestCase("../../etc/passwd")]
    [TestCase("..\\..\\windows\\evil.exe")]
    [TestCase("x./../../y")]
    [TestCase("a.txt\0.exe")]
    [TestCase("a.\u202Egnp.exe")]
    [TestCase("/etc/passwd")]
    public void NothingButAnExtensionSurvives(string fileName)
    {
        Assert.That(BlobExtension.From(fileName), Is.Empty.Or.Matches("^\\.[A-Za-z0-9]{1,10}$"), "nothing but a whitelisted extension may reach the blob path");
    }
}

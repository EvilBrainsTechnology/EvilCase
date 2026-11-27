using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileNameRulesTests
{
    [Test]
    [TestCase("a.pdf", "b.PDF", true)]
    [TestCase("a.pdf", "a.docx", false)]
    [TestCase("a", "b", true)]
    [TestCase("a.pdf", "a", false)]
    [TestCase("Smlouva.pdf.p7s", "x.p7s", true)]
    public void ANewVersionKeepsTheExtensionIgnoringCase(string fileName, string otherFileName, bool expected)
    {
        Assert.That(FileNameRules.SameExtension(fileName, otherFileName), Is.EqualTo(expected));
    }

    [TestCase("Smlouva.pdf", "Smlouva.pdf")]
    [TestCase("slozka/Smlouva.pdf", "Smlouva.pdf")]
    [TestCase(@"C:\Users\Dokumenty\Smlouva.pdf", "Smlouva.pdf")]
    [TestCase(@"..\..\Smlouva.pdf", "Smlouva.pdf")]
    [TestCase(@"a/b\Smlouva.pdf", "Smlouva.pdf")]
    public void AnUploadedNameKeepsOnlyItsLastPathSegment(string fileName, string expected)
    {
        Assert.That(FileNameRules.LastSegment(fileName), Is.EqualTo(expected), "a Windows separator cuts the path as a slash does");
    }
}

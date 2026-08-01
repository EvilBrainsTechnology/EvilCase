using System.IO.Compression;
using System.Text;
using EvilBrains.EvilCase.Import;

namespace EvilBrains.EvilCase.Tests.Import;

/// <summary>
/// Every fixture here is synthetic — bytes the test writes itself. No real document enters the
/// repository, which is public (<c>docs/product/vision.md</c>, <em>Privacy</em>).
/// </summary>
public class FileContentClassifierTests
{
    [Test]
    public void APdfIsRecognisedByItsBytesWhateverItIsCalled()
    {
        using var content = Bytes("%PDF-1.7\n%âãÏÓ\ntrailer");

        Assert.That(FileContentClassifier.Classify(content), Is.EqualTo(FileContentKind.Pdf), "the name is not an input to this at all");
    }

    [Test]
    public void AZipCarryingAWordPartIsAWordDocument()
    {
        using var content = Zip("[Content_Types].xml", "word/document.xml");

        Assert.That(FileContentClassifier.Classify(content), Is.EqualTo(FileContentKind.WordDocument));
    }

    [Test]
    public void AZipWithoutOneIsJustAZip()
    {
        using var content = Zip("readme.txt", "xl/workbook.xml");

        Assert.That(FileContentClassifier.Classify(content), Is.EqualTo(FileContentKind.Zip), "a spreadsheet is not a word document");
    }

    [Test]
    public void AnXmlDeclarationIsRecognisedThroughAByteOrderMarkAndLeadingSpace()
    {
        using var plain = Bytes("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Message/>");
        using var decorated = new MemoryStream([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("\n  <?xml version=\"1.0\"?><Message/>")]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FileContentClassifier.Classify(plain), Is.EqualTo(FileContentKind.Xml));
            Assert.That(FileContentClassifier.Classify(decorated), Is.EqualTo(FileContentKind.Xml), "a data-box envelope arrives with both");
        }
    }

    [Test]
    public void NothingMalformedThrows()
    {
        using var empty = new MemoryStream();
        using var truncatedPdf = Bytes("%PD");
        using var damagedZip = new MemoryStream([0x50, 0x4B, 0x03, 0x04, 0x11, 0x22, 0x33]);
        using var prose = Bytes("Dobry den, v priloze zasilam...");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FileContentClassifier.Classify(empty), Is.EqualTo(FileContentKind.Unknown), "an empty file is not an exception");
            Assert.That(FileContentClassifier.Classify(truncatedPdf), Is.EqualTo(FileContentKind.Unknown), "half a signature is not a signature");
            Assert.That(FileContentClassifier.Classify(damagedZip), Is.EqualTo(FileContentKind.Zip), "the signature says zip even when the rest is unreadable");
            Assert.That(FileContentClassifier.Classify(prose), Is.EqualTo(FileContentKind.Unknown), "and an unrecognised file is imported as it is, not refused");
        }
    }

    [Test]
    public void TheStreamIsLeftWhereItWasFound()
    {
        using var content = Zip("word/document.xml");
        content.Position = 2;

        _ = FileContentClassifier.Classify(content);

        Assert.That(content.Position, Is.EqualTo(2), "the caller is hashing the same stream next");
    }

    [Test]
    public void AStreamThatCannotSeekIsRefusedRatherThanHalfRead()
    {
        using var content = new NonSeekableStream();

        Assert.That(() => FileContentClassifier.Classify(content), Throws.ArgumentException, "a zip's table of contents is at its end");
    }

    private static MemoryStream Bytes(string content) => new(Encoding.UTF8.GetBytes(content));

    private static MemoryStream Zip(params string[] entryNames)
    {
        var content = new MemoryStream();

        using (var archive = new ZipArchive(content, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var entryName in entryNames)
                archive.CreateEntry(entryName);
        }

        content.Position = 0;

        return content;
    }

    private sealed class NonSeekableStream : MemoryStream
    {
        public override bool CanSeek => false;
    }
}

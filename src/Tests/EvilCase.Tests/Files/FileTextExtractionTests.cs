using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Text;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Fonts.Standard14Fonts;
using UglyToad.PdfPig.Writer;

namespace EvilBrains.EvilCase.Tests.Files;

public class FileTextExtractionTests
{
    [TestCase("zapis.txt", true)]
    [TestCase("DATA.CSV", true)]
    [TestCase("rozhodnuti.pdf", true)]
    [TestCase("podani.docx", true)]
    [TestCase("zprava.zfo", false)]
    [TestCase("sken.png", false)]
    [TestCase("podani.docx.p7m", false)]
    [TestCase("bez-pripony", false)]
    public void OnlyTextFormatsPdfAndDocxAreRead(string fileName, bool reads)
    {
        Assert.That(FileTextExtraction.Reads(fileName), Is.EqualTo(reads));
    }

    [Test]
    public void ATextFileIsReadAsUtf8WithoutItsByteOrderMark()
    {
        using var content = new MemoryStream([.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes("Žluťoučký kůň")]);

        Assert.That(FileTextExtraction.Extract("zapis.txt", content), Is.EqualTo("Žluťoučký kůň"));
    }

    [Test]
    public void APdfGivesTheTextOfItsTextLayerPageByPage()
    {
        using var content = new MemoryStream(Pdf("First page", "Second page"));

        var text = FileTextExtraction.Extract("rozhodnuti.pdf", content);

        Assert.That(text, Is.EqualTo("First page\n\nSecond page"), "pages follow each other, separated by a blank line");
    }

    [Test]
    public void APdfWithoutATextLayerGivesNoText()
    {
        using var content = new MemoryStream(Pdf());

        Assert.That(FileTextExtraction.Extract("sken.pdf", content), Is.Empty.Or.Matches<string>(string.IsNullOrWhiteSpace));
    }

    [Test]
    public void ADocxGivesItsParagraphsLineByLine()
    {
        using var content = Docx("První odstavec", "Druhý odstavec");

        Assert.That(FileTextExtraction.Extract("podani.docx", content), Is.EqualTo("První odstavec\nDruhý odstavec"));
    }

    [Test]
    public void ContentThatIsNotItsFormatThrows()
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes("not a pdf"));

        Assert.That(() => FileTextExtraction.Extract("rozhodnuti.pdf", content), Throws.Exception, "the reader answers a broken file as unreadable");
    }

    [TestCase(0, 1)]
    [TestCase(1, 1)]
    [TestCase(FileLimits.TextPageLength, 1)]
    [TestCase(FileLimits.TextPageLength + 1, 2)]
    [TestCase((3 * FileLimits.TextPageLength) - 1, 3)]
    public void APageHoldsFiftyThousandCharacters(int length, int pageCount)
    {
        Assert.That(FileTextPaging.PageCount(length), Is.EqualTo(pageCount));
    }

    [Test]
    public void PagesCutTheTextInOrderWithoutGapOrOverlap()
    {
        var text = new string('a', FileLimits.TextPageLength) + new string('b', FileLimits.TextPageLength) + "c";

        using (Assert.EnterMultipleScope())
        {
            Assert.That(FileTextPaging.Page(text, 1), Is.EqualTo(new string('a', FileLimits.TextPageLength)));
            Assert.That(FileTextPaging.Page(text, 2), Is.EqualTo(new string('b', FileLimits.TextPageLength)));
            Assert.That(FileTextPaging.Page(text, 3), Is.EqualTo("c"), "the last page holds the rest");
            Assert.That(FileTextPaging.Page(text, 4), Is.Null, "a page past the last is no page");
            Assert.That(FileTextPaging.Page(text, 0), Is.Null, "pages count from 1");
        }
    }

    private static byte[] Pdf(params string[] pages)
    {
        var builder = new PdfDocumentBuilder();
        var font = builder.AddStandard14Font(Standard14Font.Helvetica);

        if (pages.Length == 0)
            builder.AddPage(595, 842);

        foreach (var page in pages)
            builder.AddPage(595, 842).AddText(page, 12, new PdfPoint(50, 700), font);

        return builder.Build();
    }

    private static MemoryStream Docx(params string[] paragraphs)
    {
        var content = new MemoryStream();

        using (var document = WordprocessingDocument.Create(content, WordprocessingDocumentType.Document))
        {
            var main = document.AddMainDocumentPart();
            main.Document = new Document(new Body(paragraphs.Select(static text => new Paragraph(new Run(new Text(text)))).Cast<OpenXmlElement>()));
        }

        content.Position = 0;

        return content;
    }
}

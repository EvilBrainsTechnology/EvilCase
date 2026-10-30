using System.Text;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using UglyToad.PdfPig;
using UglyToad.PdfPig.DocumentLayoutAnalysis.TextExtractor;

namespace EvilBrains.EvilCase.Business.Files.Text;

/// <summary>
/// The text a file carries by its extension: a text format as stored, the text layer of a PDF, the paragraphs of a DOCX.
/// </summary>
internal static class FileTextExtraction
{
    private const string Pdf = ".pdf";

    private const string Docx = ".docx";

    private static readonly HashSet<string> PlainTextExtensions = new(
        [".txt", ".text", ".csv", ".tsv", ".md", ".json", ".xml", ".html", ".htm", ".log", ".yaml", ".yml"],
        StringComparer.OrdinalIgnoreCase);

    public static bool Reads(string fileName)
    {
        var extension = Path.GetExtension(fileName);

        return PlainTextExtensions.Contains(extension) || IsExtension(extension, Pdf) || IsExtension(extension, Docx);
    }

    /// <summary>
    /// Throws where the content does not parse as its extension says.
    /// </summary>
    public static string Extract(string fileName, Stream content)
    {
        var extension = Path.GetExtension(fileName);

        if (IsExtension(extension, Pdf))
            return PdfText(content);

        if (IsExtension(extension, Docx))
            return DocxText(content);

        // UTF-8 unless a byte order mark says otherwise.
        using var reader = new StreamReader(content, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    private static string PdfText(Stream content)
    {
        using var document = PdfDocument.Open(content);

        return string.Join("\n\n", document.GetPages().Select(static page => ContentOrderTextExtractor.GetText(page)));
    }

    private static string DocxText(Stream content)
    {
        using var document = WordprocessingDocument.Open(content, isEditable: false);

        var body = document.MainDocumentPart?.Document?.Body;

        return body is null
            ? ""
            : string.Join('\n', body.Descendants<Paragraph>().Select(static paragraph => paragraph.InnerText));
    }

    private static bool IsExtension(string extension, string expected)
    {
        return string.Equals(extension, expected, StringComparison.OrdinalIgnoreCase);
    }
}

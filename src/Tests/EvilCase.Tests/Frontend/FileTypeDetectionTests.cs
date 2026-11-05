using System.IO.Compression;
using System.Text;
using EvilBrains.EvilCase.App.Files;
using OpenMcdf;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileTypeDetectionTests
{
    private static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0, 0, 0, 0x0D];

    private static IEnumerable<TestCaseData> Contents()
    {
        yield return Case("%PDF-1.7\n"u8.ToArray(), "pdf");
        yield return Case(Png, "png");
        yield return Case([0xFF, 0xD8, 0xFF, 0xE0, 0, 0x10], "jpg");
        yield return Case("GIF89a\u0001\0"u8.ToArray(), "gif");
        yield return Case("RIFF$\0\0\0WEBPVP8 "u8.ToArray(), "webp");
        yield return Case([.. "BM"u8, 0x3A, 0, 0, 0, 0, 0, 0, 0, 0x36, 0, 0, 0, 0x28, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0, 1, 0], "bmp");
        yield return Case([0x49, 0x49, 0x2A, 0, 8, 0, 0, 0], "tif");
        yield return Case([0x4D, 0x4D, 0, 0x2A, 0, 0, 0, 8], "tif");
        yield return Case([0, 0, 0, 0x18, .. "ftypheic"u8, 0, 0, 0, 0], "heic");
        yield return Case([0, 0, 0, 0x18, .. "ftypmif1"u8, 0, 0, 0, 0], "heic");
        yield return Case([0, 0, 0, 0x18, .. "ftypavif"u8, 0, 0, 0, 0], "avif");
        yield return Case("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Potvrzeni/>"u8.ToArray(), "xml");
        yield return Case([0xEF, 0xBB, 0xBF, .. "<?xml version=\"1.0\"?><Potvrzeni/>"u8], "xml");
        yield return Case(Zip("[Content_Types].xml", "_rels/.rels", "word/document.xml"), "docx");
        yield return Case(Zip("[Content_Types].xml", "xl/workbook.xml"), "xlsx");
        yield return Case(Zip("[Content_Types].xml", "ppt/presentation.xml"), "pptx");
        yield return Case(OpenDocument("application/vnd.oasis.opendocument.spreadsheet"), "ods");
        yield return Case(OpenDocument("application/vnd.oasis.opendocument.text"), "odt");
        yield return Case(Zip("word/document.xml"), "zip");
        yield return Case(Ole("WordDocument", "1Table"), "doc");
        yield return Case(Ole("Workbook"), "xls");
        yield return Case(Ole("__substg1.0_0037001F", "__properties_version1.0"), "msg");
        yield return Case(Ole("Neznámý"), extension: null);
        yield return Case(SyntheticZfo.Message(("a.txt", [1])), "zfo");
        yield return Case("Prostý text bez typu."u8.ToArray(), extension: null);
        yield return Case("BMW a jiné značky, obyčejný text."u8.ToArray(), extension: null);
        yield return Case([], extension: null);
    }

    private static IEnumerable<TestCaseData> MediaAndArchives()
    {
        yield return Case([0, 0, 0, 0x18, .. "ftypisom"u8, 0, 0, 0, 0], "mp4");
        yield return Case([0, 0, 0, 0x18, .. "ftypM4A "u8, 0, 0, 0, 0], "m4a");
        yield return Case([0, 0, 0, 0x14, .. "ftypqt  "u8, 0, 0, 0, 0], "mov");
        yield return Case("RIFF$\0\0\0WAVEfmt "u8.ToArray(), "wav");
        yield return Case("RIFF$\0\0\0AVI LIST"u8.ToArray(), "avi");
        yield return Case([0x1A, 0x45, 0xDF, 0xA3, 0x9F, 0x42, 0x82, 0x84, .. "webm"u8], "webm");
        yield return Case([0x1A, 0x45, 0xDF, 0xA3, 0xA3, 0x42, 0x82, 0x88, .. "matroska"u8], "mkv");
        yield return Case("ID3\u0004\0\0"u8.ToArray(), "mp3");
        yield return Case("fLaC\0\0\0\u0022"u8.ToArray(), "flac");
        yield return Case("OggS\0\u0002"u8.ToArray(), "ogg");
        yield return Case("{\\rtf1\\ansi"u8.ToArray(), "rtf");
        yield return Case([0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C, 0, 4], "7z");
        yield return Case([.. "Rar!"u8, 0x1A, 0x07, 0x01, 0], "rar");
        yield return Case([0x1F, 0x8B, 0x08, 0, 0, 0, 0, 0], "gz");
    }

    [TestCaseSource(nameof(Contents))]
    [TestCaseSource(nameof(MediaAndArchives))]
    public void TheContentShowsItsExtension(byte[] content, string? extension)
    {
        Assert.That(FileTypeDetection.ExtensionOrDefault(content), Is.EqualTo(extension));
    }

    [TestCase(new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14, 0, 0, 0, 8, 0 }, "zip")]
    [TestCase(new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0, 0, 0, 0 }, null)]
    [TestCase(new byte[] { 0x30, 0x82, 0x01, 0x00, 0x06, 0x09 }, null)]
    public void ContentBrokenPastItsLeadingBytesTakesTheirTypeOrNone(byte[] content, string? extension)
    {
        Assert.That(FileTypeDetection.ExtensionOrDefault(content), Is.EqualTo(extension), "a broken container never throws");
    }

    private static TestCaseData Case(byte[] content, string? extension)
    {
        return new TestCaseData(content, extension).SetArgDisplayNames(extension ?? "none", Encoding.ASCII.GetString(content.AsSpan(0, Math.Min(content.Length, 12))));
    }

    private static byte[] Zip(params string[] names)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var name in names)
            {
                using var writer = new StreamWriter(archive.CreateEntry(name).Open());

                writer.Write("<a/>");
            }
        }

        return stream.ToArray();
    }

    private static byte[] OpenDocument(string mediaType)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var writer = new StreamWriter(archive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open()))
                writer.Write(mediaType);

            using (var writer = new StreamWriter(archive.CreateEntry("content.xml").Open()))
                writer.Write("<a/>");
        }

        return stream.ToArray();
    }

    private static byte[] Ole(params string[] streams)
    {
        using var stream = new MemoryStream();

        using (var root = RootStorage.Create(stream, OpenMcdf.Version.V3, StorageModeFlags.LeaveOpen))
        {
            foreach (var name in streams)
            {
                using var entry = root.CreateStream(name);

                entry.Write([1, 2, 3]);
            }
        }

        return stream.ToArray();
    }
}

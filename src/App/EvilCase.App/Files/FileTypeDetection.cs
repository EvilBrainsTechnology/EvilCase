using System.IO.Compression;
using System.Text;
using OpenMcdf;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The type a content shows by its leading bytes and, for a ZIP or an OLE container, by the names of its parts.
/// </summary>
internal static class FileTypeDetection
{
    private static readonly (byte[] Prefix, string Extension)[] Prefixes =
    [
        ("%PDF-"u8.ToArray(), "pdf"),
        ([0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "png"),
        ([0xFF, 0xD8, 0xFF], "jpg"),
        ("GIF87a"u8.ToArray(), "gif"),
        ("GIF89a"u8.ToArray(), "gif"),
        ([0x49, 0x49, 0x2A, 0x00], "tif"),
        ([0x4D, 0x4D, 0x00, 0x2A], "tif"),
        ("<?xml"u8.ToArray(), "xml"),
        ([0xEF, 0xBB, 0xBF, .. "<?xml"u8], "xml"),
        ("{\\rtf"u8.ToArray(), "rtf"),
        ("ID3"u8.ToArray(), "mp3"),
        ("fLaC"u8.ToArray(), "flac"),
        ("OggS"u8.ToArray(), "ogg"),
        ([0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C], "7z"),
        ([.. "Rar!"u8, 0x1A, 0x07], "rar"),
        ([0x1F, 0x8B, 0x08], "gz"),
    ];

    private static readonly byte[] EbmlPrefix = [0x1A, 0x45, 0xDF, 0xA3];

    private static readonly Dictionary<string, string> RiffForms = new(StringComparer.Ordinal)
    {
        ["WEBP"] = "webp",
        ["WAVE"] = "wav",
        ["AVI "] = "avi",
    };

    private static readonly byte[] OlePrefix = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1];

    private static readonly (string Folder, string Extension)[] OfficeOpenXmlFolders = [("word/", "docx"), ("xl/", "xlsx"), ("ppt/", "pptx")];

    // The name of a stream at the root, or the start of it, that only a container of the type holds.
    private static readonly (string Name, string Extension)[] OleStreams =
    [
        ("WordDocument", "doc"),
        ("Workbook", "xls"),
        ("Book", "xls"),
        ("PowerPoint Document", "ppt"),
        ("__substg1.0_", "msg"),
    ];

    // The major brand of an ISO media file.
    private static readonly Dictionary<string, string> FtypBrands = new(StringComparer.Ordinal)
    {
        ["heic"] = "heic",
        ["heix"] = "heic",
        ["heim"] = "heic",
        ["heis"] = "heic",
        ["hevc"] = "heic",
        ["hevx"] = "heic",
        ["mif1"] = "heic",
        ["msf1"] = "heic",
        ["avif"] = "avif",
        ["avis"] = "avif",
        ["M4A "] = "m4a",
        ["M4V "] = "m4v",
        ["qt  "] = "mov",
        ["isom"] = "mp4",
        ["iso2"] = "mp4",
        ["mp41"] = "mp4",
        ["mp42"] = "mp4",
        ["avc1"] = "mp4",
        ["dash"] = "mp4",
    };

    private static readonly Dictionary<string, string> OpenDocumentTypes = new(StringComparer.Ordinal)
    {
        ["application/vnd.oasis.opendocument.text"] = "odt",
        ["application/vnd.oasis.opendocument.spreadsheet"] = "ods",
        ["application/vnd.oasis.opendocument.presentation"] = "odp",
    };

    private static readonly int MaxOpenDocumentTypeLength = OpenDocumentTypes.Keys.Max(static type => type.Length) + 2;

    /// <summary>
    /// The extension, without the dot, of the type the content shows, or <see langword="null"/> when it shows none, as
    /// for plain text. Content that is broken past its leading bytes takes the type they show; nothing throws.
    /// </summary>
    public static string? ExtensionOrDefault(byte[] content)
    {
        var head = content.AsSpan();

        foreach (var (prefix, extension) in Prefixes)
        {
            if (head.StartsWith(prefix))
                return extension;
        }

        if (head.Length >= 12 && head.StartsWith("RIFF"u8))
            return RiffForms.GetValueOrDefault(Encoding.ASCII.GetString(head[8..12]));

        if (head.Length >= 12 && head[4..8].SequenceEqual("ftyp"u8))
            return FtypBrands.GetValueOrDefault(Encoding.ASCII.GetString(head[8..12]));

        // A Matroska file names its document type in its header.
        if (head.StartsWith(EbmlPrefix))
            return head[..Math.Min(head.Length, 64)].IndexOf("webm"u8) >= 0 ? "webm" : "mkv";

        // Two letters alone start many texts; a bitmap also has its four reserved bytes zero.
        if (head.Length >= 26 && head.StartsWith("BM"u8) && !head[6..10].ContainsAnyExcept((byte)0))
            return "bmp";

        if (head.StartsWith("PK\u0003\u0004"u8))
            return ZipExtension(content);

        if (head.StartsWith(OlePrefix))
            return OleExtension(content);

        return DataMessageZfo.ParseOrDefault(content) is null ? null : "zfo";
    }

    // An Office Open XML file names its parts in [Content_Types].xml, an OpenDocument one its type in mimetype.
    private static string ZipExtension(byte[] content)
    {
        try
        {
            using var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);

            // Only the length of the longest known type is read, whatever the entry inflates to.
            if (archive.GetEntry("mimetype") is { } mimetype)
            {
                using var reader = new StreamReader(mimetype.Open(), Encoding.ASCII);
                var type = new char[MaxOpenDocumentTypeLength];

                return OpenDocumentTypes.GetValueOrDefault(new string(type, 0, reader.ReadBlock(type)).Trim(), "zip");
            }

            if (archive.GetEntry("[Content_Types].xml") is null)
                return "zip";

            foreach (var (folder, extension) in OfficeOpenXmlFolders)
            {
                if (archive.Entries.Any(entry => entry.FullName.StartsWith(folder, StringComparison.Ordinal)))
                    return extension;
            }

            return "zip";
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return "zip";
        }
    }

    // An OLE container is a Word, Excel or PowerPoint file or an Outlook message by the streams at its root.
    private static string? OleExtension(byte[] content)
    {
        try
        {
            using var root = RootStorage.Open(new MemoryStream(content, writable: false));

            var names = root.EnumerateEntries()
                .Select(static entry => entry.Name)
                .ToList();

            return OleStreams.FirstOrDefault(stream => names.Exists(name => name.StartsWith(stream.Name, StringComparison.Ordinal))).Extension;
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            return null;
        }
    }
}

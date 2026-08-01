using System.IO.Compression;

namespace EvilBrains.EvilCase.Import;

/// <summary>
/// Decides what a file is from its content rather than from its name. A `.pdf` that is really a Word
/// document is a Word document, and a document whose name says nothing at all still classifies.
/// </summary>
/// <remarks>
/// Nothing here throws on malformed input. An importer walking someone's disk meets truncated files,
/// empty files and files that are nothing in particular, and none of those is a reason to stop the
/// import — they classify as <see cref="FileContentKind.Unknown"/> and are imported as they are.
/// </remarks>
public static class FileContentClassifier
{
    private const int HeadLength = 64;

    private static readonly byte[] PdfMagic = "%PDF-"u8.ToArray();

    private static readonly byte[] ZipMagic = [0x50, 0x4B, 0x03, 0x04];

    private static readonly byte[] XmlDeclaration = "<?xml"u8.ToArray();

    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    /// <summary>
    /// Reads from the current position and leaves the stream where it found it.
    /// </summary>
    /// <param name="content">Seekable, because a zip's table of contents is at its end.</param>
    public static FileContentKind Classify(Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (!content.CanSeek)
            throw new ArgumentException("The stream has to be seekable to be classified.", nameof(content));

        var origin = content.Position;

        try
        {
            var head = new byte[HeadLength];
            var read = content.ReadAtLeast(head, HeadLength, throwOnEndOfStream: false);

            if (head.AsSpan(0, read).StartsWith(PdfMagic))
                return FileContentKind.Pdf;

            if (head.AsSpan(0, read).StartsWith(ZipMagic))
            {
                content.Position = origin;
                return ClassifyZip(content);
            }

            return StartsWithXmlDeclaration(head, read) ? FileContentKind.Xml : FileContentKind.Unknown;
        }
        finally
        {
            content.Position = origin;
        }
    }

    private static FileContentKind ClassifyZip(Stream content)
    {
        try
        {
            using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);

            return archive.Entries.Any(entry => entry.FullName.StartsWith("word/", StringComparison.OrdinalIgnoreCase))
                ? FileContentKind.WordDocument
                : FileContentKind.Zip;
        }
        catch (InvalidDataException)
        {
            // The signature says zip and the rest is damaged. Still a zip, and still worth importing.
            return FileContentKind.Zip;
        }
    }

    private static bool StartsWithXmlDeclaration(byte[] head, int length)
    {
        var start = head.AsSpan(0, length);

        if (start.StartsWith(Utf8Bom))
            start = start[Utf8Bom.Length..];

        while (!start.IsEmpty && start[0] is (byte)' ' or (byte)'\t' or (byte)'\r' or (byte)'\n')
            start = start[1..];

        return start.StartsWith(XmlDeclaration);
    }
}

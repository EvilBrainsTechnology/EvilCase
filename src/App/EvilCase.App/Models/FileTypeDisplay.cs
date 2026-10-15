namespace EvilBrains.EvilCase.App.Models;

public static class FileTypeDisplay
{
    private const string OctetStream = "application/octet-stream";

    private static readonly Dictionary<string, FileKind> Kinds = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = FileKind.Pdf,
        ["docx"] = FileKind.Document,
        ["doc"] = FileKind.Document,
        ["odt"] = FileKind.Document,
        ["rtf"] = FileKind.Document,
        ["xlsx"] = FileKind.Sheet,
        ["xls"] = FileKind.Sheet,
        ["ods"] = FileKind.Sheet,
        ["csv"] = FileKind.Sheet,
        ["tsv"] = FileKind.Sheet,
        ["pptx"] = FileKind.Presentation,
        ["ppt"] = FileKind.Presentation,
        ["odp"] = FileKind.Presentation,
        ["txt"] = FileKind.Text,
        ["md"] = FileKind.Text,
        ["markdown"] = FileKind.Text,
        ["log"] = FileKind.Text,
        ["jpg"] = FileKind.Image,
        ["jpeg"] = FileKind.Image,
        ["png"] = FileKind.Image,
        ["gif"] = FileKind.Image,
        ["webp"] = FileKind.Image,
        ["avif"] = FileKind.Image,
        ["bmp"] = FileKind.Image,
        ["svg"] = FileKind.Image,
        ["heic"] = FileKind.Image,
        ["heif"] = FileKind.Image,
        ["tif"] = FileKind.Image,
        ["tiff"] = FileKind.Image,
        ["mp3"] = FileKind.Audio,
        ["m4a"] = FileKind.Audio,
        ["wav"] = FileKind.Audio,
        ["ogg"] = FileKind.Audio,
        ["oga"] = FileKind.Audio,
        ["flac"] = FileKind.Audio,
        ["aac"] = FileKind.Audio,
        ["mp4"] = FileKind.Video,
        ["webm"] = FileKind.Video,
        ["m4v"] = FileKind.Video,
        ["mov"] = FileKind.Video,
        ["avi"] = FileKind.Video,
        ["mkv"] = FileKind.Video,
        ["eml"] = FileKind.Email,
        ["msg"] = FileKind.Email,
        ["zfo"] = FileKind.DataMessage,
        ["p7s"] = FileKind.Signature,
        ["p7m"] = FileKind.Signature,
        ["cer"] = FileKind.Signature,
        ["crt"] = FileKind.Signature,
        ["xml"] = FileKind.Code,
        ["json"] = FileKind.Code,
        ["isdoc"] = FileKind.Code,
        ["html"] = FileKind.Code,
        ["zip"] = FileKind.Archive,
        ["7z"] = FileKind.Archive,
        ["rar"] = FileKind.Archive,
        ["gz"] = FileKind.Archive,
        ["tar"] = FileKind.Archive,
    };

    // The blob's type comes from here and never from the stored media type; a file the app decodes or a library
    // parses is never shown as a blob of its own type, so its type is unused.
    private static readonly Dictionary<string, (FilePreviewKind Kind, string MediaType)> Previews = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = (FilePreviewKind.Pdf, "application/pdf"),
        ["jpg"] = (FilePreviewKind.Image, "image/jpeg"),
        ["jpeg"] = (FilePreviewKind.Image, "image/jpeg"),
        ["png"] = (FilePreviewKind.Image, "image/png"),
        ["gif"] = (FilePreviewKind.Image, "image/gif"),
        ["webp"] = (FilePreviewKind.Image, "image/webp"),
        ["avif"] = (FilePreviewKind.Image, "image/avif"),
        ["bmp"] = (FilePreviewKind.Image, "image/bmp"),
        ["svg"] = (FilePreviewKind.Image, "image/svg+xml"),
        ["txt"] = (FilePreviewKind.Text, "text/plain"),
        ["md"] = (FilePreviewKind.Markdown, "text/markdown"),
        ["markdown"] = (FilePreviewKind.Markdown, "text/markdown"),
        ["csv"] = (FilePreviewKind.Text, "text/plain"),
        ["tsv"] = (FilePreviewKind.Text, "text/plain"),
        ["json"] = (FilePreviewKind.Text, "text/plain"),
        ["xml"] = (FilePreviewKind.Text, "text/plain"),
        ["log"] = (FilePreviewKind.Text, "text/plain"),
        ["isdoc"] = (FilePreviewKind.Text, "text/plain"),
        ["html"] = (FilePreviewKind.Text, "text/plain"),
        ["docx"] = (FilePreviewKind.Document, "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
        ["xlsx"] = (FilePreviewKind.Workbook, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
        ["xls"] = (FilePreviewKind.Workbook, "application/vnd.ms-excel"),
        ["ods"] = (FilePreviewKind.Workbook, "application/vnd.oasis.opendocument.spreadsheet"),
        ["eml"] = (FilePreviewKind.Email, "message/rfc822"),
        ["msg"] = (FilePreviewKind.Email, "application/vnd.ms-outlook"),
        ["heic"] = (FilePreviewKind.ConvertedImage, "image/heic"),
        ["heif"] = (FilePreviewKind.ConvertedImage, "image/heif"),
        ["tif"] = (FilePreviewKind.ConvertedImage, "image/tiff"),
        ["tiff"] = (FilePreviewKind.ConvertedImage, "image/tiff"),
        ["mp3"] = (FilePreviewKind.Audio, "audio/mpeg"),
        ["m4a"] = (FilePreviewKind.Audio, "audio/mp4"),
        ["wav"] = (FilePreviewKind.Audio, "audio/wav"),
        ["ogg"] = (FilePreviewKind.Audio, "audio/ogg"),
        ["oga"] = (FilePreviewKind.Audio, "audio/ogg"),
        ["mp4"] = (FilePreviewKind.Video, "video/mp4"),
        ["webm"] = (FilePreviewKind.Video, "video/webm"),
        ["m4v"] = (FilePreviewKind.Video, "video/mp4"),
        ["mov"] = (FilePreviewKind.Video, "video/quicktime"),
    };

    public static FileKind Kind(string fileName)
    {
        return Kinds.GetValueOrDefault(Extension(fileName), FileKind.Other);
    }

    public static FilePreviewKind Preview(string fileName)
    {
        return Previews.TryGetValue(Extension(fileName), out var preview) ? preview.Kind : FilePreviewKind.None;
    }

    public static string PreviewMediaType(string fileName)
    {
        return Previews.TryGetValue(Extension(fileName), out var preview) ? preview.MediaType : OctetStream;
    }

    private static string Extension(string fileName)
    {
        return Path.GetExtension(fileName).TrimStart('.');
    }
}

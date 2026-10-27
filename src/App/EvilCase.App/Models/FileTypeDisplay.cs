namespace EvilBrains.EvilCase.App.Models;

public static class FileTypeDisplay
{
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

    private static readonly Dictionary<string, FilePreviewKind> Previews = new(StringComparer.OrdinalIgnoreCase)
    {
        ["pdf"] = FilePreviewKind.Pdf,
        ["jpg"] = FilePreviewKind.Image,
        ["jpeg"] = FilePreviewKind.Image,
        ["png"] = FilePreviewKind.Image,
        ["gif"] = FilePreviewKind.Image,
        ["webp"] = FilePreviewKind.Image,
        ["avif"] = FilePreviewKind.Image,
        ["bmp"] = FilePreviewKind.Image,
        ["svg"] = FilePreviewKind.Image,
        ["txt"] = FilePreviewKind.Text,
        ["md"] = FilePreviewKind.Markdown,
        ["markdown"] = FilePreviewKind.Markdown,
        ["csv"] = FilePreviewKind.Text,
        ["tsv"] = FilePreviewKind.Text,
        ["json"] = FilePreviewKind.Text,
        ["xml"] = FilePreviewKind.Text,
        ["log"] = FilePreviewKind.Text,
        ["isdoc"] = FilePreviewKind.Text,
        ["html"] = FilePreviewKind.Text,
        ["docx"] = FilePreviewKind.Document,
        ["xlsx"] = FilePreviewKind.Workbook,
        ["xls"] = FilePreviewKind.Workbook,
        ["ods"] = FilePreviewKind.Workbook,
        ["eml"] = FilePreviewKind.Email,
        ["msg"] = FilePreviewKind.Email,
        ["zfo"] = FilePreviewKind.DataMessage,
        ["p7m"] = FilePreviewKind.SignedDocument,
        ["p7s"] = FilePreviewKind.SignedDocument,
        ["heic"] = FilePreviewKind.ConvertedImage,
        ["heif"] = FilePreviewKind.ConvertedImage,
        ["mp3"] = FilePreviewKind.Audio,
        ["m4a"] = FilePreviewKind.Audio,
        ["wav"] = FilePreviewKind.Audio,
        ["ogg"] = FilePreviewKind.Audio,
        ["oga"] = FilePreviewKind.Audio,
        ["mp4"] = FilePreviewKind.Video,
        ["webm"] = FilePreviewKind.Video,
        ["m4v"] = FilePreviewKind.Video,
        ["mov"] = FilePreviewKind.Video,
    };

    public static FileKind Kind(string fileName)
    {
        return Kinds.GetValueOrDefault(Extension(fileName), FileKind.Other);
    }

    public static FilePreviewKind Preview(string fileName)
    {
        return Previews.GetValueOrDefault(Extension(fileName));
    }

    /// <summary>
    /// The name of the document a signed file (p7m, p7s) carries: its own name without the signature suffix.
    /// </summary>
    public static string SignedContentName(string fileName)
    {
        return Path.GetFileNameWithoutExtension(fileName);
    }

    private static string Extension(string fileName)
    {
        return Path.GetExtension(fileName).TrimStart('.');
    }
}

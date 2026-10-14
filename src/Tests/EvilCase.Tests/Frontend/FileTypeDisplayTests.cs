using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileTypeDisplayTests
{
    [TestCase("a.pdf", FileKind.Pdf)]
    [TestCase("a.DOCX", FileKind.Document)]
    [TestCase("a.odt", FileKind.Document)]
    [TestCase("a.xlsx", FileKind.Sheet)]
    [TestCase("a.csv", FileKind.Sheet)]
    [TestCase("a.pptx", FileKind.Presentation)]
    [TestCase("a.md", FileKind.Text)]
    [TestCase("a.heic", FileKind.Image)]
    [TestCase("a.tiff", FileKind.Image)]
    [TestCase("a.ogg", FileKind.Audio)]
    [TestCase("a.mov", FileKind.Video)]
    [TestCase("a.msg", FileKind.Email)]
    [TestCase("a.zfo", FileKind.DataMessage)]
    [TestCase("a.p7m", FileKind.Signature)]
    [TestCase("a.crt", FileKind.Signature)]
    [TestCase("a.isdoc", FileKind.Code)]
    [TestCase("a.7z", FileKind.Archive)]
    [TestCase("a.unknown", FileKind.Other)]
    [TestCase("README", FileKind.Other)]
    public void TheIconFollowsTheExtension(string fileName, FileKind kind)
    {
        Assert.That(FileTypeDisplay.Kind(fileName), Is.EqualTo(kind));
    }

    [TestCase("a.pdf", FilePreviewKind.Pdf)]
    [TestCase("a.jpg", FilePreviewKind.Image)]
    [TestCase("a.jpeg", FilePreviewKind.Image)]
    [TestCase("a.png", FilePreviewKind.Image)]
    [TestCase("a.gif", FilePreviewKind.Image)]
    [TestCase("a.webp", FilePreviewKind.Image)]
    [TestCase("a.avif", FilePreviewKind.Image)]
    [TestCase("a.bmp", FilePreviewKind.Image)]
    [TestCase("a.svg", FilePreviewKind.Image)]
    [TestCase("a.txt", FilePreviewKind.Text)]
    [TestCase("a.md", FilePreviewKind.Text)]
    [TestCase("a.csv", FilePreviewKind.Text)]
    [TestCase("a.tsv", FilePreviewKind.Text)]
    [TestCase("a.json", FilePreviewKind.Text)]
    [TestCase("a.xml", FilePreviewKind.Text)]
    [TestCase("a.log", FilePreviewKind.Text)]
    [TestCase("a.isdoc", FilePreviewKind.Text)]
    [TestCase("a.html", FilePreviewKind.Text)]
    [TestCase("a.mp3", FilePreviewKind.Audio)]
    [TestCase("a.m4a", FilePreviewKind.Audio)]
    [TestCase("a.wav", FilePreviewKind.Audio)]
    [TestCase("a.oga", FilePreviewKind.Audio)]
    [TestCase("a.mp4", FilePreviewKind.Video)]
    [TestCase("a.webm", FilePreviewKind.Video)]
    [TestCase("a.m4v", FilePreviewKind.Video)]
    [TestCase("a.docx", FilePreviewKind.None)]
    [TestCase("a.heic", FilePreviewKind.None)]
    [TestCase("a.zfo", FilePreviewKind.None)]
    [TestCase("README", FilePreviewKind.None)]
    public void TheBrowserPreviewsOnlyTheFormatsItCanShow(string fileName, FilePreviewKind kind)
    {
        Assert.That(FileTypeDisplay.Preview(fileName), Is.EqualTo(kind));
    }

    [TestCase("a.pdf", "application/pdf")]
    [TestCase("a.SVG", "image/svg+xml")]
    [TestCase("a.m4a", "audio/mp4")]
    [TestCase("a.mp4", "video/mp4")]
    [TestCase("a.docx", "application/octet-stream")]
    public void TheBlobTypeComesFromTheExtension(string fileName, string mediaType)
    {
        Assert.That(FileTypeDisplay.PreviewMediaType(fileName), Is.EqualTo(mediaType));
    }
}

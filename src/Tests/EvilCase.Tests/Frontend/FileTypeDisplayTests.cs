using EvilBrains.EvilCase.Api.Contract.Files;
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
    [TestCase("a.md", FilePreviewKind.Markdown)]
    [TestCase("a.MARKDOWN", FilePreviewKind.Markdown)]
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
    [TestCase("a.docx", FilePreviewKind.Document)]
    [TestCase("a.xlsx", FilePreviewKind.Workbook)]
    [TestCase("a.xls", FilePreviewKind.Workbook)]
    [TestCase("a.ODS", FilePreviewKind.Workbook)]
    [TestCase("a.eml", FilePreviewKind.Email)]
    [TestCase("a.msg", FilePreviewKind.Email)]
    [TestCase("a.heic", FilePreviewKind.ConvertedImage)]
    [TestCase("a.heif", FilePreviewKind.ConvertedImage)]
    [TestCase("a.tif", FilePreviewKind.None)]
    [TestCase("a.TIFF", FilePreviewKind.None)]
    [TestCase("a.doc", FilePreviewKind.None)]
    [TestCase("a.odt", FilePreviewKind.None)]
    [TestCase("a.pptx", FilePreviewKind.None)]
    [TestCase("a.zfo", FilePreviewKind.DataMessage)]
    [TestCase("a.ZFO", FilePreviewKind.DataMessage)]
    [TestCase("README", FilePreviewKind.None)]
    public void OnlyTheFormatsTheAppCanShowOpenInThePreview(string fileName, FilePreviewKind kind)
    {
        Assert.That(FileTypeDisplay.Preview(fileName), Is.EqualTo(kind));
    }

    [TestCase("a.pdf", null)]
    [TestCase("a.PNG", "image/png")]
    [TestCase("a.m4a", "audio/mp4")]
    [TestCase("a.mp4", "video/mp4")]
    [TestCase("a.svg", null)]
    [TestCase("a.html", null)]
    [TestCase("a.heic", null)]
    [TestCase("a.zip", null)]
    public void OnlyWhatTheBrowserShowsNativelyHasAnInlineType(string fileName, string? mediaType)
    {
        Assert.That(InlineFileTypes.MediaType(fileName), Is.EqualTo(mediaType));
    }

    [Test]
    public void EveryInlineTypeHasAPreviewTheBrowserShowsItself()
    {
        var nativePreviews = new[] { FilePreviewKind.Image, FilePreviewKind.Audio, FilePreviewKind.Video };

        foreach (var extension in new[] { "jpg", "jpeg", "png", "gif", "webp", "avif", "bmp", "mp3", "m4a", "wav", "ogg", "oga", "mp4", "webm", "m4v", "mov" })
            Assert.That(nativePreviews, Does.Contain(FileTypeDisplay.Preview($"a.{extension}")), extension);
    }
}

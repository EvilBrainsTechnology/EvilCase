namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// How the preview shows a file: natively in the browser, or parsed by a library the browser loads on first use.
/// </summary>
public enum FilePreviewKind
{
    None = 0,

    Pdf = 1,

    Image = 2,

    Text = 3,

    Audio = 4,

    Video = 5,

    Markdown = 6,

    Document = 7,

    Workbook = 8,

    Email = 9,

    ConvertedImage = 10,

    DataMessage = 11,

    /// <summary>
    /// A CMS signature (p7m, p7s): the document it carries, or its signers where detached.
    /// </summary>
    SignedDocument = 12,
}

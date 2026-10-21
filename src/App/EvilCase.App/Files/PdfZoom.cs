namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A zoom of the PDF preview: a step in or out, a fit to the preview, or the actual size.
/// </summary>
internal enum PdfZoom
{
    In = 0,

    Out = 1,

    PageWidth = 2,

    PageFit = 3,

    Actual = 4,
}

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A zoom of the PDF preview: a step in or out, or a fit to the preview.
/// </summary>
internal enum PdfZoom
{
    In = 0,

    Out = 1,

    PageWidth = 2,

    PageFit = 3,
}

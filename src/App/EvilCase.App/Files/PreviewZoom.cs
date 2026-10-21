namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A zoom of a preview: a step in or out, a fit to the preview, or the actual size.
/// </summary>
public enum PreviewZoom
{
    In = 0,

    Out = 1,

    PageWidth = 2,

    PageFit = 3,

    Actual = 4,
}

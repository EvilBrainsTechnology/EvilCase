namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Which match of a search in the PDF preview to show: the first one of a changed query, or the next or previous one.
/// </summary>
internal enum PdfFind
{
    New = 0,

    Next = 1,

    Previous = 2,
}

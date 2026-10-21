namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Which match of a search in a preview to show: the first one of a changed query, or the next or previous one.
/// </summary>
public enum PreviewFind
{
    New = 0,

    Next = 1,

    Previous = 2,
}

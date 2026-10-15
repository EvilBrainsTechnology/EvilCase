namespace EvilBrains.EvilCase.App.Search;

/// <summary>
/// A stretch of a text, marked where it is what a search found.
/// </summary>
internal readonly record struct HighlightSegment(string Text, bool Match);

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A search in a preview: the query, empty to clear the highlights, and which of its matches to show.
/// </summary>
public readonly record struct PreviewSearch(string Query, PreviewFind Find);

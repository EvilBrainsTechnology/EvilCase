namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// How a compare of two texts shows: whether the texts show formatted, whether both format, the lines added and removed
/// and the number of changes.
/// </summary>
public sealed record CompareView(bool Formatted, bool Formattable, int Added, int Removed, int Changes);

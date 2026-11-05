namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A shown column: its position in the sheet and its width in CSS pixels.
/// </summary>
public sealed record SheetColumn(int Index, int Width);

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// How a cell is drawn: the classes of the app's sheet styles it takes, and the declarations of the colours, sizes and
/// borders its workbook gives it, or <see langword="null"/> where it gives none.
/// </summary>
public sealed record SheetStyle(string ClassName, string? Css);

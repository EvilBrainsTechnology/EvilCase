namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// An XLSX cell format as the preview draws it: the classes of its font, vertical alignment and wrapping, its explicit
/// horizontal alignment (<see langword="null"/> for the general one), the declarations of its colours, size and indent,
/// its fill colour, and the CSS of each border side it has.
/// </summary>
internal sealed record XlsxCellFormat(string ClassName, string? Horizontal, string Css, string? Fill, string? Left, string? Right, string? Top, string? Bottom);

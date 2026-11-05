namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A drawn cell: its text, its index into <see cref="SheetPreview.Styles"/>, and the shown columns and rows it spans.
/// </summary>
public sealed record SheetCell(string Text, int Style, int ColumnSpan, int RowSpan);

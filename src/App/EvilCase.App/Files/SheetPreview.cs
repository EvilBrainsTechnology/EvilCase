namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// One worksheet as the preview shows it: the shown columns and rows of its first <see cref="RowsRead"/> rows, the
/// styles their cells take, and the row count of the whole sheet.
/// </summary>
public sealed record SheetPreview(string Name, IReadOnlyList<SheetColumn> Columns, IReadOnlyList<SheetRow> Rows, IReadOnlyList<SheetStyle> Styles, int RowsRead, int RowCount);

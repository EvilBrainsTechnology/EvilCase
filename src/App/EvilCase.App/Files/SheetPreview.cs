namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// One worksheet as the preview shows it: the cell texts of the first rows, and the size of the whole sheet.
/// </summary>
internal sealed record SheetPreview(string Name, IReadOnlyList<IReadOnlyList<string>> Rows, int RowCount, int ColumnCount);

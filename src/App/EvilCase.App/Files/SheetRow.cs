namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// A shown row: its position in the sheet, its height in CSS pixels where the workbook gives one, and a cell for each
/// shown column, <see langword="null"/> where a merged cell covers it.
/// </summary>
public sealed record SheetRow(int Index, int? Height, IReadOnlyList<SheetCell?> Cells);

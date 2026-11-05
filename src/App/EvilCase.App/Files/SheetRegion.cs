namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The range of a drawn cell in the sheet, a merged one wider than one cell; the next shown row and column after it,
/// -1 for none; and whether it is drawn in the first shown row or column.
/// </summary>
internal readonly record struct SheetRegion(int Row, int Column, int LastRow, int LastColumn, int NextRow, int NextColumn, bool FirstRow, bool FirstColumn);

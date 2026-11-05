using ExcelDataReader;

namespace EvilBrains.EvilCase.App.Files;

// The read area of a sheet laid out the way Excel draws it: hidden columns and rows left out, a merged range one cell.
internal static partial class WorkbookReader
{
    internal const string AlignCenter = "ec-preview-sheet-center";

    internal const string AlignRight = "ec-preview-sheet-right";

    internal const string AlignJustify = "ec-preview-sheet-justify";

    internal const string AlignTop = "ec-preview-sheet-top";

    internal const string AlignMiddle = "ec-preview-sheet-middle";

    // Excel's default column of 8.43 characters.
    private const double DefaultColumnCharacters = 8.43;

    private const int DefaultColumnWidth = 64;

    // A column width stored by Excel counts digits of the default font, 7 pixels wide, padding included.
    private const double DigitWidth = 7;

    internal static string ClassNames(params string[] classNames)
    {
        return string.Join(' ', classNames.Where(static className => className.Length > 0));
    }

    // The indent level of a cell, which the sheet's stylesheet turns into padding.
    internal static string Indent(int level)
    {
        return string.Create(CultureInfo.InvariantCulture, $"--ec-preview-sheet-cell-indent:{level}");
    }

    /// <summary>
    /// The sheet from the width of each column (0 where hidden), the height of each read row (0 where hidden), the text
    /// of each cell with the alignment its value takes, and the merged ranges. A drawn cell's style comes from its
    /// region and its value's alignment.
    /// </summary>
    private static SheetPreview LayOut(
        string name,
        int rowCount,
        IReadOnlyList<int> columnWidths,
        IReadOnlyList<int?> rowHeights,
        IReadOnlyList<(string Text, string Align)[]> cells,
        IEnumerable<CellRange> merges,
        Func<SheetRegion, string, SheetStyle> style)
    {
        var (shownColumns, columnsBefore, nextColumns) = Shown(columnWidths.Count, column => columnWidths[column] > 0);
        var (shownRows, rowsBefore, nextRows) = Shown(cells.Count, row => rowHeights[row] != 0);
        var (drawn, covered) = DrawnMerges(merges, cells.Count - 1, columnWidths.Count - 1, (shownRows, rowsBefore), (shownColumns, columnsBefore));

        var styles = new List<SheetStyle>();
        var styleIndexes = new Dictionary<SheetStyle, int>();
        var rows = new List<SheetRow>(shownRows.Count);

        foreach (var row in shownRows)
        {
            var rowCells = new SheetCell?[shownColumns.Count];

            for (var shown = 0; shown < shownColumns.Count; shown++)
            {
                var column = shownColumns[shown];

                if (!drawn.TryGetValue((row, column), out var range))
                {
                    if (covered.Contains((row, column)))
                        continue;

                    range = new CellRange(column, row, column, row);
                }

                var (text, align) = cells[range.FromRow][range.FromColumn];
                var region = new SheetRegion(range.FromRow, range.FromColumn, range.ToRow, range.ToColumn, nextRows[range.ToRow], nextColumns[range.ToColumn], row == shownRows[0], shown == 0);
                var cellStyle = style(region, align);

                if (!styleIndexes.TryGetValue(cellStyle, out var index))
                {
                    index = styles.Count;
                    styleIndexes[cellStyle] = index;
                    styles.Add(cellStyle);
                }

                rowCells[shown] = new SheetCell(text, index, columnsBefore[range.ToColumn + 1] - shown, rowsBefore[range.ToRow + 1] - rowsBefore[row]);
            }

            rows.Add(new SheetRow(row, rowHeights[row], rowCells));
        }

        var columns = shownColumns.ConvertAll(column => new SheetColumn(column, columnWidths[column]));

        return new SheetPreview(name, columns, rows, styles, cells.Count, rowCount);
    }

    // Each merge within the read area by the position it is drawn at, its first shown row and column, and the positions
    // it covers.
    private static (Dictionary<(int Row, int Column), CellRange> Drawn, HashSet<(int Row, int Column)> Covered) DrawnMerges(
        IEnumerable<CellRange> merges,
        int lastReadRow,
        int lastReadColumn,
        (List<int> Shown, int[] Before) rows,
        (List<int> Shown, int[] Before) columns)
    {
        var drawn = new Dictionary<(int Row, int Column), CellRange>();
        var covered = new HashSet<(int Row, int Column)>();

        foreach (var merge in merges)
        {
            var lastRow = Math.Min(merge.ToRow, lastReadRow);
            var lastColumn = Math.Min(merge.ToColumn, lastReadColumn);

            if (merge.FromRow > lastRow || merge.FromColumn > lastColumn || (merge.FromRow == lastRow && merge.FromColumn == lastColumn))
                continue;

            for (var row = merge.FromRow; row <= lastRow; row++)
            {
                for (var column = merge.FromColumn; column <= lastColumn; column++)
                    covered.Add((row, column));
            }

            var firstRow = rows.Before[merge.FromRow] < rows.Shown.Count ? rows.Shown[rows.Before[merge.FromRow]] : int.MaxValue;
            var firstColumn = columns.Before[merge.FromColumn] < columns.Shown.Count ? columns.Shown[columns.Before[merge.FromColumn]] : int.MaxValue;

            if (firstRow <= lastRow && firstColumn <= lastColumn)
                drawn[(firstRow, firstColumn)] = new CellRange(merge.FromColumn, merge.FromRow, lastColumn, lastRow);
        }

        return (drawn, covered);
    }

    // The shown positions, the count of shown ones before each position and after the last, and the next shown one
    // after each, -1 for none.
    private static (List<int> Shown, int[] Before, int[] Next) Shown(int count, Func<int, bool> isShown)
    {
        var shown = new List<int>();
        var before = new int[count + 1];
        var next = new int[count];

        for (var position = 0; position < count; position++)
        {
            before[position] = shown.Count;

            if (isShown(position))
                shown.Add(position);
        }

        before[count] = shown.Count;

        for (int position = count - 1, following = -1; position >= 0; position--)
        {
            next[position] = following;

            if (isShown(position))
                following = position;
        }

        return (shown, before, next);
    }

    // A width Excel stores in characters, in CSS pixels as Excel draws it at 100 %.
    private static int ColumnWidth(double characters)
    {
        if (characters <= 0)
            return 0;

        if (Math.Abs(characters - DefaultColumnCharacters) < 0.001)
            return DefaultColumnWidth;

        return Math.Max(1, (int)(((256 * characters) + Math.Truncate(128 / DigitWidth)) / 256 * DigitWidth));
    }
}

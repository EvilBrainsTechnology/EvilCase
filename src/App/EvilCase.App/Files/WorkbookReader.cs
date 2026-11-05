using System.Text;
using ExcelDataReader;
using ExcelNumberFormat;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Reads the shown sheets of an XLSX or XLS workbook for the preview, told apart by the content. Each sheet starts
/// at A1 and shows at most 100 columns.
/// </summary>
internal static partial class WorkbookReader
{
    internal const int MaxColumns = 100;

    // The short date and the short date with time Excel shows in the reader's locale.
    private const int ShortDateFormat = 14;

    private const int ShortDateTimeFormat = 22;

    private static readonly CultureInfo Czech = CzechCulture();

    static WorkbookReader()
    {
        // An XLS file names its text by a code page.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    /// <summary>
    /// Every shown sheet of the workbook, no more than <paramref name="maxRows"/> rows of each, or <see langword="null"/>
    /// for content that is no workbook or cannot be read.
    /// </summary>
    public static IReadOnlyList<SheetPreview>? ReadOrDefault(byte[] content, int maxRows)
    {
        var isZip = content is [0x50, 0x4b, ..];
        var isOle = content is [0xd0, 0xcf, 0x11, 0xe0, ..];

        if (!isZip && !isOle)
            return null;

        try
        {
            return ReadExcel(content, isZip, maxRows);
        }
        catch (Exception)
        {
            // A broken file throws whatever the reader meets first.
            return null;
        }
    }

    private static List<SheetPreview> ReadExcel(byte[] content, bool isZip, int maxRows)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var reader = ExcelReaderFactory.CreateReader(stream);
        using var styles = isZip ? XlsxStyles.OpenOrDefault(content) : null;

        var sheets = new List<SheetPreview>();
        var formats = new Dictionary<string, NumberFormat>(StringComparer.Ordinal);

        do
        {
            if (!string.Equals(reader.VisibleState, "visible", StringComparison.Ordinal))
                continue;

            var rowCount = reader.RowCount;
            var columnCount = Math.Min(reader.FieldCount, MaxColumns);

            if (rowCount == 0 || columnCount == 0)
            {
                sheets.Add(new SheetPreview(reader.Name, [], [], [], 0, 0));

                continue;
            }

            var columnWidths = Enumerable.Range(0, columnCount)
                .Select(column => ColumnWidth(reader.GetColumnWidth(column)))
                .ToList();
            var xlsx = styles?.Sheet(reader.Name, Math.Min(rowCount, maxRows));
            var rowHeights = new List<int?>();
            var cells = new List<(string Text, string Align)[]>();
            var cellStyles = new List<CellStyle[]>();

            while (cells.Count < maxRows && reader.Read())
            {
                var row = new (string Text, string Align)[columnCount];

                for (var column = 0; column < columnCount; column++)
                    row[column] = CellText(reader, column, formats);

                if (xlsx is null)
                    cellStyles.Add([.. Enumerable.Range(0, columnCount).Select(reader.GetCellStyle)]);

                rowHeights.Add(xlsx?.RowHeight(cells.Count) ?? Pixels(reader.RowHeight));
                cells.Add(row);
            }

            var style = xlsx?.Style ?? ((region, align) => ExcelStyle(cellStyles[region.Row][region.Column], align));

            sheets.Add(LayOut(reader.Name, rowCount, columnWidths, rowHeights, cells, reader.MergeCells, style));
        }
        while (reader.NextResult());

        return sheets;
    }

    // The text Excel shows for the cell by its number format, and the alignment Excel gives its value: text to the left,
    // a number or a date to the right, a logical value or an error centred.
    private static (string Text, string Align) CellText(IExcelDataReader reader, int column, Dictionary<string, NumberFormat> formats)
    {
        var value = reader.GetValue(column);

        if (value is null)
            return reader.GetCellError(column) is { } error ? (ErrorText(error), AlignCenter) : ("", "");

        if (value is bool truth)
            return (truth ? "PRAVDA" : "NEPRAVDA", AlignCenter);

        var formatString = reader.GetNumberFormatIndex(column) switch
        {
            ShortDateFormat => "d. m. yyyy",
            ShortDateTimeFormat => "d. m. yyyy h:mm",
            _ => reader.GetNumberFormatString(column, CultureInfo.InvariantCulture) ?? "General",
        };

        if (!formats.TryGetValue(formatString, out var format))
        {
            format = new NumberFormat(formatString);
            formats[formatString] = format;
        }

        var text = format.IsValid ? format.Format(value, Czech) : Convert.ToString(value, Czech) ?? "";

        return (text, value is string ? "" : AlignRight);
    }

    // The alignment and indent the Excel reader gives a cell, all an XLS shows; its fonts, fills and borders are not read.
    private static SheetStyle ExcelStyle(CellStyle style, string align)
    {
        var horizontal = style.HorizontalAlignment switch
        {
            HorizontalAlignment.General => align,
            HorizontalAlignment.Center or HorizontalAlignment.Centered or HorizontalAlignment.CenteredAcrossSelection => AlignCenter,
            HorizontalAlignment.Right => AlignRight,
            HorizontalAlignment.Justified or HorizontalAlignment.Distributed => AlignJustify,
            HorizontalAlignment.Left or HorizontalAlignment.Filled => "",
            _ => align,
        };

        var vertical = style.VerticalAlignment switch
        {
            VerticalAlignment.Top or VerticalAlignment.Justify or VerticalAlignment.Distributed => AlignTop,
            VerticalAlignment.Center => AlignMiddle,
            VerticalAlignment.Bottom => "",
            _ => "",
        };

        return new SheetStyle(ClassNames(horizontal, vertical), style.IndentLevel > 0 ? Indent(style.IndentLevel) : null);
    }

    // A height in points as pixels; only a hidden row is 0.
    internal static int Pixels(double points)
    {
        return points > 0 ? Math.Max(1, (int)Math.Round(points * 4 / 3, MidpointRounding.AwayFromZero)) : 0;
    }

    private static string ErrorText(CellError error)
    {
        return error switch
        {
            CellError.NULL => "#NULL!",
            CellError.DIV0 => "#DIV/0!",
            CellError.VALUE => "#VALUE!",
            CellError.REF => "#REF!",
            CellError.NAME => "#NAME?",
            CellError.NUM => "#NUM!",
            CellError.NA => "#N/A",
            CellError.GETTING_DATA => "#GETTING_DATA",
            _ => "",
        };
    }

    // The browser may load no Czech culture data; the separators hold either way.
    private static CultureInfo CzechCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("cs-CZ").Clone();

        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = "\u00A0";

        return CultureInfo.ReadOnly(culture);
    }
}

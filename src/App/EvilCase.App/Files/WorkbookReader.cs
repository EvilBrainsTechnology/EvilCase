using System.Text;
using ExcelDataReader;
using ExcelNumberFormat;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// Reads the sheets of an XLSX, XLS or ODS workbook for the preview, told apart by the content. Each sheet starts at A1
/// and shows at most 100 columns.
/// </summary>
internal static partial class WorkbookReader
{
    private const int MaxColumns = 100;

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
    /// Every sheet of the workbook, no more than <paramref name="maxRows"/> rows of each, or <see langword="null"/> for
    /// content that is no workbook or cannot be read.
    /// </summary>
    public static IReadOnlyList<SheetPreview>? ReadOrDefault(byte[] content, int maxRows)
    {
        var isZip = content is [0x50, 0x4b, ..];
        var isOle = content is [0xd0, 0xcf, 0x11, 0xe0, ..];

        if (!isZip && !isOle)
            return null;

        try
        {
            if (isZip && OpenSpreadsheet(content) is { } spreadsheet)
            {
                using (spreadsheet)
                    return ReadOpenDocument(spreadsheet, maxRows);
            }

            return ReadExcel(content, maxRows);
        }
        catch (Exception)
        {
            // A broken file throws whatever the reader meets first.
            return null;
        }
    }

    private static List<SheetPreview> ReadExcel(byte[] content, int maxRows)
    {
        using var stream = new MemoryStream(content, writable: false);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        var sheets = new List<SheetPreview>();
        var formats = new Dictionary<string, NumberFormat>(StringComparer.Ordinal);

        do
        {
            var rowCount = reader.RowCount;
            var columnCount = Math.Min(reader.FieldCount, MaxColumns);

            if (rowCount == 0 || columnCount == 0)
            {
                sheets.Add(new SheetPreview(reader.Name, [], 0, 0));

                continue;
            }

            var rows = new List<string[]>();

            while (rows.Count < maxRows && reader.Read())
            {
                var row = new string[columnCount];

                for (var column = 0; column < columnCount; column++)
                    row[column] = CellText(reader, column, formats);

                rows.Add(row);
            }

            sheets.Add(new SheetPreview(reader.Name, rows, rowCount, columnCount));
        }
        while (reader.NextResult());

        return sheets;
    }

    // The text Excel shows for the cell by its number format.
    private static string CellText(IExcelDataReader reader, int column, Dictionary<string, NumberFormat> formats)
    {
        var value = reader.GetValue(column);

        if (value is null)
            return reader.GetCellError(column) is { } error ? ErrorText(error) : "";

        if (value is bool truth)
            return truth ? "PRAVDA" : "NEPRAVDA";

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

        return format.IsValid ? format.Format(value, Czech) : Convert.ToString(value, Czech) ?? "";
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

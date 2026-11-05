using System.IO.Compression;
using System.Text;
using System.Xml;
using ExcelDataReader;

namespace EvilBrains.EvilCase.App.Files;

// An ODS spreadsheet: each cell shows the text its application wrote beside the value; column widths, row heights,
// hidden columns, rows and sheets and merged cells come from the content, the cell styles are not read.
internal static partial class WorkbookReader
{
    private const string SpreadsheetMediaType = "application/vnd.oasis.opendocument.spreadsheet";

    private const string OfficeNamespace = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

    private const string StyleNamespace = "urn:oasis:names:tc:opendocument:xmlns:style:1.0";

    private const string TableNamespace = "urn:oasis:names:tc:opendocument:xmlns:table:1.0";

    private const string TextNamespace = "urn:oasis:names:tc:opendocument:xmlns:text:1.0";

    // A run of spaces longer than any cell shows.
    private const int MaxSpaces = 1000;

    private static readonly XmlReaderSettings OpenDocumentSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
        IgnoreComments = true,
        IgnoreProcessingInstructions = true,
    };

    // The archive when it is an ODS spreadsheet; any other ZIP is left to the Excel reader.
    private static ZipArchive? OpenSpreadsheet(byte[] content)
    {
        var archive = new ZipArchive(new MemoryStream(content, writable: false), ZipArchiveMode.Read);

        if (archive.GetEntry("mimetype") is { } entry)
        {
            using var mimetype = new StreamReader(entry.Open(), Encoding.ASCII);

            if (string.Equals(mimetype.ReadToEnd().Trim(), SpreadsheetMediaType, StringComparison.Ordinal))
                return archive;
        }

        archive.Dispose();

        return null;
    }

    // The automatic styles come before the sheets in the content.
    private static List<SheetPreview> ReadOpenDocument(ZipArchive spreadsheet, int maxRows)
    {
        var entry = spreadsheet.GetEntry("content.xml") ?? throw new InvalidDataException("The spreadsheet has no content.");

        using var content = entry.Open();
        using var xml = XmlReader.Create(content, OpenDocumentSettings);

        var lengths = new Dictionary<(string Family, string Name), int>();
        var hiddenSheets = new HashSet<string>(StringComparer.Ordinal);
        var sheets = new List<SheetPreview>();

        while (xml.Read())
        {
            if (IsElement(xml, StyleNamespace, "style"))
            {
                ReadOpenDocumentStyle(xml, lengths, hiddenSheets);
            }
            else if (IsElement(xml, TableNamespace, "table"))
            {
                // A hidden sheet is passed over to its end.
                if (hiddenSheets.Contains(xml.GetAttribute("style-name", TableNamespace) ?? ""))
                    xml.ReadSubtree().Dispose();
                else
                    sheets.Add(ReadOpenDocumentSheet(xml, maxRows, lengths));
            }
        }

        return sheets;
    }

    // A column width or row height in pixels, or a sheet the application does not display.
    private static void ReadOpenDocumentStyle(XmlReader xml, Dictionary<(string Family, string Name), int> lengths, HashSet<string> hiddenSheets)
    {
        var name = xml.GetAttribute("name", StyleNamespace) ?? "";
        var family = xml.GetAttribute("family", StyleNamespace) ?? "";

        using var style = xml.ReadSubtree();

        while (style.Read())
        {
            if (IsElement(style, StyleNamespace, "table-column-properties") && Length(style.GetAttribute("column-width", StyleNamespace)) is { } width)
                lengths[(family, name)] = width;
            else if (IsElement(style, StyleNamespace, "table-row-properties") && Length(style.GetAttribute("row-height", StyleNamespace)) is { } height)
                lengths[(family, name)] = height;
            else if (IsElement(style, StyleNamespace, "table-properties") && string.Equals(style.GetAttribute("display", TableNamespace), "false", StringComparison.Ordinal))
                hiddenSheets.Add(name);
        }
    }

    // Rows, columns and cells carry repeat counts; the empty ones the application writes up to the sheet's end count for
    // nothing.
    private static SheetPreview ReadOpenDocumentSheet(XmlReader xml, int maxRows, Dictionary<(string Family, string Name), int> lengths)
    {
        var name = xml.GetAttribute("name", TableNamespace) ?? "";
        var columnWidths = new List<int>();
        var rows = new List<(int? Height, Dictionary<int, (string Text, string Align)> Cells)>();
        var merges = new List<CellRange>();
        long rowIndex = 0;
        long rowCount = 0;
        var columnCount = 0;

        using (var sheet = xml.ReadSubtree())
        {
            while (sheet.Read())
            {
                // A column or a row finds its width or height under its own name as the style family.
                var length = lengths.TryGetValue((sheet.LocalName, sheet.GetAttribute("style-name", TableNamespace) ?? ""), out var styled) ? styled : (int?)null;

                if (IsElement(sheet, TableNamespace, "table-column"))
                {
                    for (var copy = Repeat(sheet, "number-columns-repeated"); copy > 0 && columnWidths.Count < MaxColumns; copy--)
                        columnWidths.Add(IsCollapsed(sheet) ? 0 : length ?? DefaultColumnWidth);
                }
                else if (IsElement(sheet, TableNamespace, "table-row"))
                {
                    var repeat = Repeat(sheet, "number-rows-repeated");
                    var height = IsCollapsed(sheet) ? 0 : length;
                    var (cells, spans, width) = ReadOpenDocumentRow(sheet);

                    if (width > 0)
                    {
                        rowCount = rowIndex + repeat;
                        columnCount = Math.Max(columnCount, width);
                    }

                    for (var copy = rowIndex; copy < Math.Min(rowIndex + repeat, maxRows); copy++)
                    {
                        rows.Add((height, cells));

                        foreach (var (column, columnSpan, rowSpan) in spans)
                            merges.Add(new CellRange(column, (int)copy, column + columnSpan - 1, (int)copy + rowSpan - 1));
                    }

                    rowIndex += repeat;
                }
            }
        }

        var read = rows.Take((int)Math.Min(rowCount, maxRows)).ToList();

        var texts = read.ConvertAll(row => Enumerable.Range(0, columnCount)
            .Select(column => row.Cells.GetValueOrDefault(column, ("", "")))
            .ToArray());
        var widths = columnWidths.Concat(Enumerable.Repeat(DefaultColumnWidth, columnCount))
            .Take(columnCount)
            .ToList();

        return LayOut(name, (int)Math.Min(rowCount, int.MaxValue), widths, read.ConvertAll(static row => row.Height), texts, merges, static (_, align) => new SheetStyle(align, Css: null));
    }

    // The non-empty cells of the row within the shown columns with the alignment their value takes, the cells spanning
    // more than one, and the width up to its last non-empty cell.
    private static (Dictionary<int, (string Text, string Align)> Cells, List<(int Column, int ColumnSpan, int RowSpan)> Spans, int Width) ReadOpenDocumentRow(XmlReader xml)
    {
        var cells = new Dictionary<int, (string Text, string Align)>();
        var spans = new List<(int Column, int ColumnSpan, int RowSpan)>();
        var column = 0;
        var width = 0;

        using var row = xml.ReadSubtree();

        row.Read();

        while (row.Read() && column < MaxColumns)
        {
            if (row.Depth != 1 || !(IsElement(row, TableNamespace, "table-cell") || IsElement(row, TableNamespace, "covered-table-cell")))
                continue;

            var end = (int)Math.Min(column + (long)Repeat(row, "number-columns-repeated"), MaxColumns);
            var columnSpan = Repeat(row, "number-columns-spanned");
            var rowSpan = Repeat(row, "number-rows-spanned");

            var align = row.GetAttribute("value-type", OfficeNamespace) switch
            {
                "float" or "percentage" or "currency" or "date" or "time" => AlignRight,
                "boolean" => AlignCenter,
                _ => "",
            };

            var text = ReadOpenDocumentCell(row);

            for (var index = column; index < end; index++)
            {
                if (text.Length > 0)
                    cells[index] = (text, align);

                if (columnSpan > 1 || rowSpan > 1)
                    spans.Add((index, columnSpan, rowSpan));
            }

            if (text.Length > 0)
                width = end;

            column = end;
        }

        return (cells, spans, width);
    }

    // The paragraphs of the cell; its comment and the shapes anchored to it are left out.
    private static string ReadOpenDocumentCell(XmlReader xml)
    {
        if (xml.IsEmptyElement)
            return "";

        var text = new StringBuilder();
        var paragraphs = 0;

        using var cell = xml.ReadSubtree();

        cell.Read();
        cell.Read();

        while (!cell.EOF)
        {
            if (cell.Depth == 1 && cell.NodeType == XmlNodeType.Element && !IsElement(cell, TextNamespace, "p"))
            {
                cell.Skip();

                continue;
            }

            if (IsElement(cell, TextNamespace, "p") && paragraphs++ > 0)
                text.Append('\n');
            else if (IsElement(cell, TextNamespace, "line-break"))
                text.Append('\n');
            else if (IsElement(cell, TextNamespace, "tab"))
                text.Append('\t');
            else if (IsElement(cell, TextNamespace, "s"))
                text.Append(' ', Math.Min(Repeat(cell, "c", TextNamespace), MaxSpaces));
            else if (cell.Depth > 1 && cell.NodeType is XmlNodeType.Text or XmlNodeType.Whitespace or XmlNodeType.SignificantWhitespace)
                text.Append(cell.Value);

            cell.Read();
        }

        return text.ToString();
    }

    private static bool IsElement(XmlReader xml, string elementNamespace, string localName)
    {
        return xml.NodeType == XmlNodeType.Element
            && string.Equals(xml.LocalName, localName, StringComparison.Ordinal)
            && string.Equals(xml.NamespaceURI, elementNamespace, StringComparison.Ordinal);
    }

    // A column or row hidden by the user or by a filter.
    private static bool IsCollapsed(XmlReader xml)
    {
        return xml.GetAttribute("visibility", TableNamespace) is "collapse" or "filter";
    }

    private static int Repeat(XmlReader xml, string attribute, string attributeNamespace = TableNamespace)
    {
        return int.TryParse(xml.GetAttribute(attribute, attributeNamespace), NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count > 1
            ? count
            : 1;
    }

    // A length such as 2.258cm in CSS pixels, or null for one without a known unit.
    private static int? Length(string? length)
    {
        var unit = length?.TrimStart("0123456789.".ToCharArray()) ?? "";
        var factor = unit switch
        {
            "cm" => 96 / 2.54,
            "mm" => 96 / 25.4,
            "in" => 96,
            "pt" => 4 / 3.0,
            "pc" => 16,
            "px" => 1,
            _ => 0,
        };

        return factor > 0 && double.TryParse(length![..^unit.Length], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var value)
            ? (int)Math.Round(value * factor, MidpointRounding.AwayFromZero)
            : null;
    }
}

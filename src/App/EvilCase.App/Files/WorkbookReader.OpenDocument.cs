using System.IO.Compression;
using System.Text;
using System.Xml;

namespace EvilBrains.EvilCase.App.Files;

// An ODS spreadsheet: each cell shows the text its application wrote beside the value.
internal static partial class WorkbookReader
{
    private const string SpreadsheetMediaType = "application/vnd.oasis.opendocument.spreadsheet";

    private const string OfficeNamespace = "urn:oasis:names:tc:opendocument:xmlns:office:1.0";

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
        IgnoreWhitespace = true,
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

    private static List<SheetPreview> ReadOpenDocument(ZipArchive spreadsheet, int maxRows)
    {
        var entry = spreadsheet.GetEntry("content.xml") ?? throw new InvalidDataException("The spreadsheet has no content.");

        using var content = entry.Open();
        using var xml = XmlReader.Create(content, OpenDocumentSettings);

        var sheets = new List<SheetPreview>();

        while (xml.ReadToFollowing("table", TableNamespace))
            sheets.Add(ReadOpenDocumentSheet(xml, maxRows));

        return sheets;
    }

    // Rows and cells carry repeat counts; the empty ones the application writes up to the sheet's end count for nothing.
    private static SheetPreview ReadOpenDocumentSheet(XmlReader xml, int maxRows)
    {
        var name = xml.GetAttribute("name", TableNamespace) ?? "";
        var shown = new List<IReadOnlyList<(int Column, string Text)>>();
        long rowIndex = 0;
        long rowCount = 0;
        var columnCount = 0;

        using (var sheet = xml.ReadSubtree())
        {
            while (sheet.Read())
            {
                if (!IsElement(sheet, TableNamespace, "table-row"))
                    continue;

                var repeat = Repeat(sheet, "number-rows-repeated");
                var (cells, width) = ReadOpenDocumentRow(sheet);

                if (width > 0)
                {
                    rowCount = rowIndex + repeat;
                    columnCount = Math.Max(columnCount, width);

                    for (var copy = rowIndex; copy < Math.Min(rowCount, maxRows); copy++)
                    {
                        while (shown.Count < copy)
                            shown.Add([]);

                        shown.Add(cells);
                    }
                }

                rowIndex += repeat;
            }
        }

        var rows = shown.Select(cells =>
        {
            var row = Enumerable.Repeat("", columnCount).ToArray();

            foreach (var (column, text) in cells)
                row[column] = text;

            return row;
        });

        return new SheetPreview(name, [.. rows], (int)Math.Min(rowCount, int.MaxValue), columnCount);
    }

    // The non-empty cells of the row within the shown columns, and the width up to its last non-empty one.
    private static (List<(int Column, string Text)> Cells, int Width) ReadOpenDocumentRow(XmlReader xml)
    {
        var cells = new List<(int Column, string Text)>();
        var column = 0;
        var width = 0;

        using var row = xml.ReadSubtree();

        row.Read();

        while (row.Read() && column < MaxColumns)
        {
            if (row.Depth != 1 || !(IsElement(row, TableNamespace, "table-cell") || IsElement(row, TableNamespace, "covered-table-cell")))
                continue;

            var end = (int)Math.Min(column + (long)Repeat(row, "number-columns-repeated"), MaxColumns);
            var text = ReadOpenDocumentCell(row);

            if (text.Length > 0)
            {
                for (var index = column; index < end; index++)
                    cells.Add((index, text));

                width = end;
            }

            column = end;
        }

        return (cells, width);
    }

    // The paragraphs of the cell, its comment left out.
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
            if (IsElement(cell, OfficeNamespace, "annotation"))
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
            else if (cell.NodeType is XmlNodeType.Text or XmlNodeType.SignificantWhitespace)
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

    private static int Repeat(XmlReader xml, string attribute, string attributeNamespace = TableNamespace)
    {
        return int.TryParse(xml.GetAttribute(attribute, attributeNamespace), NumberStyles.None, CultureInfo.InvariantCulture, out var count) && count > 1
            ? count
            : 1;
    }
}

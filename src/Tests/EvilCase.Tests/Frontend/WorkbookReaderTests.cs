using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class WorkbookReaderTests
{
    private const uint ShortDate = 1;

    private const uint Percent = 2;

    private const uint Grouped = 3;

    private const string Ods = """
        <office:document-content xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
            xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0" xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0"
            xmlns:draw="urn:oasis:names:tc:opendocument:xmlns:drawing:1.0">
          <office:body><office:spreadsheet>
            <table:table table:name="Přehled">
              <table:table-column table:number-columns-repeated="1024"/>
              <table:table-row table:number-rows-repeated="2"><table:table-cell table:number-columns-repeated="1024"/></table:table-row>
              <table:table-row>
                <table:table-cell/>
                <table:table-cell office:value-type="float" office:value="1234.5"><text:p>1 234,50</text:p></table:table-cell>
                <table:table-cell table:number-columns-repeated="2" office:value-type="string"><text:p>a<text:s text:c="2"/>b</text:p><text:p>c</text:p></table:table-cell>
                <table:table-cell office:value-type="date" office:date-value="2026-11-05"><office:annotation><text:p>Poznámka</text:p></office:annotation><text:p>5. 11. 2026</text:p></table:table-cell>
                <table:table-cell table:number-columns-repeated="1019"/>
              </table:table-row>
              <table:table-row table:number-rows-repeated="3"><table:table-cell><draw:custom-shape><text:p>Tvar</text:p></draw:custom-shape><text:p><text:span>x</text:span> <text:span>y</text:span></text:p></table:table-cell></table:table-row>
              <table:table-row table:number-rows-repeated="1048567"><table:table-cell table:number-columns-repeated="1024"/></table:table-row>
            </table:table>
            <table:table table:name="Prázdný"><table:table-row table:number-rows-repeated="1048576"><table:table-cell table:number-columns-repeated="1024"/></table:table-row></table:table>
          </office:spreadsheet></office:body>
        </office:document-content>
        """;

    [Test]
    public void AnXlsxCellShowsItsValueByItsNumberFormatTheCzechWay()
    {
        var content = Xlsx((
            "List1",
            [
                ("A1", "Text", 0),
                ("B1", 1.5, 0),
                ("C1", 1234.5, Grouped),
                ("D1", 0.125, Percent),
                ("E1", new DateTime(2026, 11, 5).ToOADate(), ShortDate),
                ("F1", true, 0),
            ]));

        var sheet = WorkbookReader.ReadOrDefault(content, 10)!.Single();

        Assert.That(Lines(sheet), Is.EqualTo(["Text|1,5|1\u00A0234,50|12,50%|5. 11. 2026|PRAVDA"]), "a cell shows what Czech Excel shows, the short date as d. M. yyyy");
    }

    [Test]
    public void AnXlsxSheetStartsAtA1WhereverItsCellsStart()
    {
        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", [("C3", "x", 0)])), 10)!.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Lines(sheet), Is.EqualTo(["||", "||", "||x"]), "the headings match Excel only from A1");
            Assert.That((sheet.RowCount, sheet.ColumnCount), Is.EqualTo((3, 3)));
        }
    }

    [Test]
    public void AnXlsxSheetShowsTheRowsAskedForAndAtMostAHundredColumnsButCountsAllRows()
    {
        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", [("A1", 1.0, 0), ("A2", 2.0, 0), ("A3", 3.0, 0), ("A4", 4.0, 0), ("EU5", "far", 0)])), 2)!.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheet.Rows, Has.Count.EqualTo(2));
            Assert.That(sheet.Rows.Select(static row => row.Count), Is.All.EqualTo(100), "a row holds the hundred shown columns");
            Assert.That((sheet.RowCount, sheet.ColumnCount), Is.EqualTo((5, 100)), "the row count is the sheet's, the column count the shown one");
        }
    }

    [Test]
    public void AnEmptyXlsxSheetHasNoRows()
    {
        var sheets = WorkbookReader.ReadOrDefault(Xlsx(("Data", [("A1", "a", 0)]), ("Prázdný", [])), 10)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheets.Select(static sheet => sheet.Name), Is.EqualTo(["Data", "Prázdný"]));
            Assert.That((sheets[1].Rows.Count, sheets[1].RowCount, sheets[1].ColumnCount), Is.EqualTo((0, 0, 0)));
        }
    }

    [Test]
    public void AnXlsReadsItsSheetsAndFormatsLikeAnXlsx()
    {
        var content = File.ReadAllBytes(Path.Combine(TestContext.CurrentContext.TestDirectory, "Frontend", "Workbooks", "workbook.xls"));

        var sheets = WorkbookReader.ReadOrDefault(content, 10)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheets.Select(static sheet => sheet.Name), Is.EqualTo(["Data", "Empty"]));
            Assert.That(Lines(sheets[0]), Is.EqualTo(["Text|1\u00A0234,50|12,5%|5. 11. 2026", "||1,5|"]), "a code-page workbook reads its texts and number formats");
            Assert.That(sheets[1].Rows, Is.Empty);
        }
    }

    [Test]
    public void AnOdsCellShowsTheTextItsApplicationWroteAndTheRepeatsUpToTheSheetsEndCountForNothing()
    {
        var sheets = WorkbookReader.ReadOrDefault(OdsArchive(Ods), 5)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheets.Select(static sheet => sheet.Name), Is.EqualTo(["Přehled", "Prázdný"]));
            Assert.That(Lines(sheets[0]), Is.EqualTo(["||||", "||||", "|1 234,50|a  b\nc|a  b\nc|5. 11. 2026", "x y||||", "x y||||"]), "a repeated cell or row shows each time, a space between spans holds and a comment or shape is no part of the cell");
            Assert.That((sheets[0].RowCount, sheets[0].ColumnCount), Is.EqualTo((6, 5)), "the empty rows and columns after the last value are not counted");
            Assert.That((sheets[1].Rows.Count, sheets[1].RowCount, sheets[1].ColumnCount), Is.EqualTo((0, 0, 0)), "a sheet of empty cells is empty");
        }
    }

    [Test]
    public void ContentThatIsNoWorkbookReadsAsNone()
    {
        using var other = new MemoryStream();

        using (var archive = new ZipArchive(other, ZipArchiveMode.Create, leaveOpen: true))
        {
            using var writer = new StreamWriter(archive.CreateEntry("readme.txt").Open());
            writer.Write("not a workbook");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(WorkbookReader.ReadOrDefault("Sloupec;Hodnota\na;1"u8.ToArray(), 10), Is.Null, "text is no workbook even under a workbook's name");
            Assert.That(WorkbookReader.ReadOrDefault(other.ToArray(), 10), Is.Null, "a ZIP holding no workbook is none");
            Assert.That(WorkbookReader.ReadOrDefault([0xd0, 0xcf, 0x11, 0xe0, 1, 2, 3], 10), Is.Null, "a broken container is none");
        }
    }

    private static IEnumerable<string> Lines(SheetPreview sheet)
    {
        return sheet.Rows.Select(static row => string.Join('|', row));
    }

    private static byte[] OdsArchive(string contentXml)
    {
        using var stream = new MemoryStream();

        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var mimetype = new StreamWriter(archive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open(), Encoding.ASCII))
                mimetype.Write("application/vnd.oasis.opendocument.spreadsheet");

            using var content = new StreamWriter(archive.CreateEntry("content.xml").Open(), Encoding.UTF8);
            content.Write(contentXml);
        }

        return stream.ToArray();
    }

    private static byte[] Xlsx(params (string Name, (string Reference, object Value, uint Style)[] Cells)[] sheets)
    {
        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new Sheets());

            var stylesPart = workbookPart.AddNewPart<WorkbookStylesPart>();
            stylesPart.Stylesheet = new Stylesheet(
                new NumberingFormats(new NumberingFormat { NumberFormatId = 164, FormatCode = "#,##0.00" }),
                new Fonts(new Font()),
                new Fills(new Fill()),
                new Borders(new Border()),
                new CellFormats(
                    new CellFormat { NumberFormatId = 0 },
                    new CellFormat { NumberFormatId = 14, ApplyNumberFormat = true },
                    new CellFormat { NumberFormatId = 10, ApplyNumberFormat = true },
                    new CellFormat { NumberFormatId = 164, ApplyNumberFormat = true }));

            uint sheetId = 1;

            foreach (var (name, cells) in sheets)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
                var data = new SheetData();

                foreach (var row in cells.GroupBy(static cell => uint.Parse(string.Concat(cell.Reference.Where(char.IsDigit)), CultureInfo.InvariantCulture)))
                    data.Append(new Row(row.Select(static cell => Cell(cell.Reference, cell.Value, cell.Style))) { RowIndex = row.Key });

                worksheetPart.Worksheet = new Worksheet(data);
                workbookPart.Workbook.Sheets!.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId++, Name = name });
            }
        }

        return stream.ToArray();
    }

    private static Cell Cell(string reference, object value, uint style)
    {
        var cell = new Cell { CellReference = reference, StyleIndex = style };

        if (value is string text)
        {
            cell.DataType = CellValues.InlineString;
            cell.InlineString = new InlineString(new Text(text));
        }
        else if (value is bool truth)
        {
            cell.DataType = CellValues.Boolean;
            cell.CellValue = new CellValue(truth ? "1" : "0");
        }
        else
        {
            cell.CellValue = new CellValue((double)value);
        }

        return cell;
    }
}

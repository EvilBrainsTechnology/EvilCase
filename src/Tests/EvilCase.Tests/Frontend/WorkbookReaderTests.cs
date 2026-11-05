using System.IO.Compression;
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class WorkbookReaderTests
{
    private const string Right = "ec-preview-sheet-right";

    private const string Center = "ec-preview-sheet-center";

    // Cell formats 1–3 are number formats, 4–9 the styles the style test reads.
    private const string Styles = """
        <styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">
          <numFmts count="1"><numFmt numFmtId="164" formatCode="#,##0.00"/></numFmts>
          <fonts>
            <font><sz val="11"/><color theme="1"/><name val="Calibri"/></font>
            <font><b/><i/><u/><strike/><sz val="11"/><color rgb="FFC00000"/></font>
            <font><sz val="22"/><color indexed="10"/></font>
            <font><sz val="11"/><color theme="3"/></font>
          </fonts>
          <fills>
            <fill><patternFill patternType="none"/></fill>
            <fill><patternFill patternType="gray125"/></fill>
            <fill><patternFill patternType="solid"><fgColor theme="4" tint="0.79998168889431442"/><bgColor indexed="64"/></patternFill></fill>
          </fills>
          <borders>
            <border><left/><right/><top/><bottom/><diagonal/></border>
            <border><left style="medium"><color rgb="FF00B050"/></left><right style="thin"><color auto="1"/></right><top/><bottom style="double"/><diagonal/></border>
            <border><left style="dashed"><color indexed="12"/></left><right/><top/><bottom/><diagonal/></border>
          </borders>
          <cellXfs>
            <xf/>
            <xf numFmtId="14" applyNumberFormat="1"/>
            <xf numFmtId="10" applyNumberFormat="1"/>
            <xf numFmtId="164" applyNumberFormat="1"/>
            <xf fontId="1" applyFont="1"/>
            <xf fillId="2" applyFill="1" applyAlignment="1"><alignment horizontal="center" wrapText="1"/></xf>
            <xf borderId="1" applyBorder="1" applyAlignment="1"><alignment vertical="top" indent="2"/></xf>
            <xf fontId="2" applyFont="1"/>
            <xf borderId="2" applyBorder="1"/>
            <xf fontId="3" applyFont="1"/>
          </cellXfs>
        </styleSheet>
        """;

    private const string Theme = """
        <a:theme xmlns:a="http://schemas.openxmlformats.org/drawingml/2006/main" name="Office"><a:themeElements><a:clrScheme name="Office">
          <a:dk1><a:sysClr val="windowText" lastClr="000000"/></a:dk1><a:lt1><a:sysClr val="window" lastClr="FFFFFF"/></a:lt1>
          <a:dk2><a:srgbClr val="44546A"/></a:dk2><a:lt2><a:srgbClr val="E7E6E6"/></a:lt2>
          <a:accent1><a:srgbClr val="4472C4"/></a:accent1><a:accent2><a:srgbClr val="ED7D31"/></a:accent2>
          <a:accent3><a:srgbClr val="A5A5A5"/></a:accent3><a:accent4><a:srgbClr val="FFC000"/></a:accent4>
          <a:accent5><a:srgbClr val="5B9BD5"/></a:accent5><a:accent6><a:srgbClr val="70AD47"/></a:accent6>
          <a:hlink><a:srgbClr val="0563C1"/></a:hlink><a:folHlink><a:srgbClr val="954F72"/></a:folHlink>
        </a:clrScheme></a:themeElements></a:theme>
        """;

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

    private const string OdsLayout = """
        <office:document-content xmlns:office="urn:oasis:names:tc:opendocument:xmlns:office:1.0"
            xmlns:style="urn:oasis:names:tc:opendocument:xmlns:style:1.0" xmlns:table="urn:oasis:names:tc:opendocument:xmlns:table:1.0"
            xmlns:text="urn:oasis:names:tc:opendocument:xmlns:text:1.0">
          <office:automatic-styles>
            <style:style style:name="co1" style:family="table-column"><style:table-column-properties style:column-width="2.54cm"/></style:style>
            <style:style style:name="ro1" style:family="table-row"><style:table-row-properties style:row-height="30pt"/></style:style>
            <style:style style:name="ta2" style:family="table"><style:table-properties table:display="false"/></style:style>
          </office:automatic-styles>
          <office:body><office:spreadsheet>
            <table:table table:name="Skrytý" table:style-name="ta2"><table:table-row><table:table-cell><text:p>x</text:p></table:table-cell></table:table-row></table:table>
            <table:table table:name="Viditelný">
              <table:table-column table:style-name="co1"/>
              <table:table-column table:style-name="co1" table:visibility="collapse"/>
              <table:table-column table:number-columns-repeated="2"/>
              <table:table-row table:style-name="ro1">
                <table:table-cell table:number-columns-spanned="3" table:number-rows-spanned="2" office:value-type="string"><text:p>Spojeno</text:p></table:table-cell>
                <table:covered-table-cell table:number-columns-repeated="2"/>
                <table:table-cell office:value-type="float" office:value="7"><text:p>7</text:p></table:table-cell>
              </table:table-row>
              <table:table-row table:visibility="collapse"><table:covered-table-cell table:number-columns-repeated="3"/><table:table-cell><text:p>skrytý</text:p></table:table-cell></table:table-row>
              <table:table-row><table:table-cell office:value-type="boolean" office:boolean-value="true"><text:p>PRAVDA</text:p></table:table-cell></table:table-row>
            </table:table>
          </office:spreadsheet></office:body>
        </office:document-content>
        """;

    [Test]
    public void AnXlsxCellShowsItsValueByItsNumberFormatTheCzechWayAlignedByItsValue()
    {
        const string worksheet = """
            <sheetData><row r="1">
              <c r="A1" t="inlineStr"><is><t>Text</t></is></c><c r="B1"><v>1.5</v></c><c r="C1" s="3"><v>1234.5</v></c>
              <c r="D1" s="2"><v>0.125</v></c><c r="E1" s="1"><v>46331</v></c><c r="F1" t="b"><v>1</v></c><c r="G1" t="e"><v>#DIV/0!</v></c>
            </row></sheetData>
            """;

        var content = Xlsx(("List1", null, worksheet));

        var sheet = WorkbookReader.ReadOrDefault(content, 10)!.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Lines(sheet), Is.EqualTo(["Text|1,5|1\u00A0234,50|12,50%|5. 11. 2026|PRAVDA|#DIV/0!"]), "a cell shows what Czech Excel shows, the short date as d. M. yyyy");
            Assert.That(sheet.Rows[0].Cells.Select(cell => sheet.Styles[cell!.Style].ClassName), Is.EqualTo(["", Right, Right, Right, Right, Center, Center]), "text aligns left, a number or date right, a logical value or an error in the centre");
        }
    }

    [Test]
    public void AnXlsxSheetStartsAtA1WhereverItsCellsStart()
    {
        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", null, """<sheetData><row r="3"><c r="C3" t="inlineStr"><is><t>x</t></is></c></row></sheetData>""")), 10)!.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Lines(sheet), Is.EqualTo(["||", "||", "||x"]), "the headings match Excel only from A1");
            Assert.That((sheet.RowCount, sheet.Columns.Count), Is.EqualTo((3, 3)));
        }
    }

    [Test]
    public void AnXlsxSheetShowsTheRowsAskedForAndAtMostAHundredColumnsButCountsAllRows()
    {
        const string worksheet = """
            <sheetData>
              <row r="1"><c r="A1"><v>1</v></c></row><row r="2"><c r="A2"><v>2</v></c></row><row r="3"><c r="A3"><v>3</v></c></row>
              <row r="4"><c r="A4"><v>4</v></c></row><row r="5"><c r="EU5" t="inlineStr"><is><t>far</t></is></c></row>
            </sheetData>
            """;

        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", null, worksheet)), 2)!.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheet.Rows, Has.Count.EqualTo(2));
            Assert.That(sheet.Rows.Select(static row => row.Cells.Count), Is.All.EqualTo(100), "a row holds the hundred shown columns");
            Assert.That((sheet.RowsRead, sheet.RowCount, sheet.Columns.Count), Is.EqualTo((2, 5, 100)), "the row count is the sheet's, the columns the shown ones");
        }
    }

    [Test]
    public void AnEmptyXlsxSheetHasNoRowsAndAHiddenSheetIsLeftOut()
    {
        var sheets = WorkbookReader.ReadOrDefault(
            Xlsx(
                ("Data", null, """<sheetData><row r="1"><c r="A1" t="inlineStr"><is><t>a</t></is></c></row></sheetData>"""),
                ("Skrytý", SheetStateValues.Hidden, """<sheetData><row r="1"><c r="A1"><v>1</v></c></row></sheetData>"""),
                ("Prázdný", null, "<sheetData/>"),
                ("Velmi skrytý", SheetStateValues.VeryHidden, """<sheetData><row r="1"><c r="A1"><v>1</v></c></row></sheetData>""")),
            10)!;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheets.Select(static sheet => sheet.Name), Is.EqualTo(["Data", "Prázdný"]), "a hidden sheet shows as little as in Excel");
            Assert.That((sheets[1].Rows.Count, sheets[1].RowCount, sheets[1].Columns.Count), Is.EqualTo((0, 0, 0)));
        }
    }

    [Test]
    public void AnXlsxSheetLeavesOutHiddenRowsAndColumnsKeepingTheirNumbersAndTakesItsWidthsAndHeights()
    {
        const string worksheet = """
            <cols><col min="1" max="1" width="20" customWidth="1"/><col min="2" max="2" width="12" hidden="1"/><col min="4" max="4" width="10.7109375" customWidth="1"/></cols>
            <sheetData>
              <row r="1"><c r="A1"><v>1</v></c><c r="B1"><v>2</v></c><c r="D1"><v>4</v></c></row>
              <row r="2" hidden="1"><c r="A2"><v>5</v></c></row>
              <row r="3" ht="30" customHeight="1"><c r="A3"><v>6</v></c></row>
            </sheetData>
            """;

        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", null, worksheet)), 10)!.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheet.Columns.Select(static column => (column.Index, column.Width)), Is.EqualTo([(0, 140), (2, 64), (3, 75)]), "a width in characters takes Excel's pixels, the default 64, and a hidden column none");
            Assert.That(sheet.Rows.Select(static row => (row.Index, row.Height)), Is.EqualTo(new (int, int?)[] { (0, 20), (2, 40) }), "a hidden row is left out and a row's height in points shows in pixels");
            Assert.That(Lines(sheet), Is.EqualTo(["1||4", "6||"]));
        }
    }

    [Test]
    public void AnXlsxMergeSpansItsShownColumnsAndRowsWithinTheReadArea()
    {
        const string worksheet = """
            <cols><col min="2" max="2" width="12" hidden="1"/></cols>
            <sheetData>
              <row r="1"><c r="A1" t="inlineStr"><is><t>Sloučeno</t></is></c><c r="D1" t="inlineStr"><is><t>x</t></is></c><c r="CU1" t="inlineStr"><is><t>široký</t></is></c><c r="CZ1"><v>1</v></c></row>
              <row r="2" hidden="1"><c r="D2" t="inlineStr"><is><t>skrytý počátek</t></is></c></row>
              <row r="3"><c r="C3" t="inlineStr"><is><t>dlouhý</t></is></c></row>
              <row r="4"/><row r="5"/><row r="9"><c r="A9"><v>9</v></c></row>
            </sheetData>
            <mergeCells><mergeCell ref="A1:C2"/><mergeCell ref="D2:D3"/><mergeCell ref="C3:C9"/><mergeCell ref="CU1:CX1"/></mergeCells>
            """;

        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", null, worksheet)), 4)!.Single();

        var cells = Cells(sheet);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cells[0].Take(3), Is.EqualTo(["Sloučeno:2x1", "-", "x:1x1"]), "a merge spans only its shown columns and rows and covers the rest");
            Assert.That(cells[0].TakeLast(3), Is.EqualTo([":1x1", "široký:2x1", "-"]), "a merge past the hundredth column spans only the shown ones");
            Assert.That(cells[1].Take(3), Is.EqualTo([":1x1", "dlouhý:1x2", "skrytý počátek:1x1"]), "a merge reaching past the rows read spans only those, one starting in a hidden row shows at its first shown one");
            Assert.That(cells[2].Take(3), Is.EqualTo([":1x1", "-", ":1x1"]));
        }
    }

    [Test]
    public void AnXlsxCellTakesItsFontFillBorderAndAlignmentWithThemeColoursTinted()
    {
        const string worksheet = """
            <sheetData>
              <row r="1">
                <c r="A1" s="4" t="inlineStr"><is><t>a</t></is></c><c r="B1" s="5"><v>5</v></c><c r="C1" s="6" t="inlineStr"><is><t>c</t></is></c>
                <c r="D1" t="inlineStr"><is><t>d</t></is></c><c r="E1" s="8" t="inlineStr"><is><t>e</t></is></c>
              </row>
              <row r="2"><c r="A2" s="7" t="inlineStr"><is><t>velké</t></is></c><c r="B2" s="9" t="inlineStr"><is><t>tmavé</t></is></c></row>
              <row r="4" s="5" customFormat="1"><c r="A4"><v>4</v></c></row>
            </sheetData>
            """;

        var sheet = WorkbookReader.ReadOrDefault(Xlsx(("List1", null, worksheet)), 10)!.Single();

        SheetStyle Style(int row, int column)
        {
            return sheet.Styles[sheet.Rows[row].Cells[column]!.Style];
        }

        IEnumerable<string> Declarations(int row, int column)
        {
            return (Style(row, column).Css ?? "").Split(';');
        }

        var fill = Declarations(0, 1).Single(static declaration => declaration.StartsWith("background-color:#", StringComparison.Ordinal))[^6..];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Style(0, 0).ClassName, Is.EqualTo("ec-preview-sheet-bold ec-preview-sheet-italic ec-preview-sheet-underline ec-preview-sheet-strike"));
            Assert.That(Style(0, 0).Css, Is.EqualTo("color:#C00000;border-right-color:#" + fill), "the fill of the cell after hides the grid line between them");
            Assert.That(Style(0, 1).ClassName, Is.EqualTo("ec-preview-sheet-center ec-preview-sheet-wrap"), "an explicit alignment overrides the one the value takes");
            Assert.That(Channels(fill), Is.EqualTo(Channels("D9E1F2")).Within(2), "a theme colour resolves with its tint the way Excel shows accent 1 lighter by 80 %");
            Assert.That(Declarations(0, 1), Does.Contain("border-right:2px solid #00B050").And.Contain("border-bottom-color:#" + fill), "the left border of the cell after draws as the right one, a fill hides its other grid line");
            Assert.That(
                Style(0, 2),
                Is.EqualTo(new SheetStyle("ec-preview-sheet-top", "--ec-preview-sheet-cell-indent:2;border-right:1px solid #000000;border-bottom:3px double #000000")),
                "a cell's own left border draws only in the first column, an automatic colour is black");
            Assert.That(Declarations(0, 3), Does.Contain("border-right:1px dashed #0000FF"), "an indexed colour comes from Excel's palette");
            Assert.That(Style(1, 0).Css, Is.EqualTo("color:#FF0000;font-size:2em"), "a font size is relative to the workbook's default one");
            Assert.That(Style(1, 1).Css, Is.EqualTo("color:#44546A"), "theme colour 3 is the second dark one");
            Assert.That((Style(3, 0).ClassName, Style(3, 1).ClassName), Is.EqualTo((Right, "ec-preview-sheet-center ec-preview-sheet-wrap")), "a cell without a style takes the default one, an absent cell its row's");
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
            Assert.That(sheets[0].Styles[sheets[0].Rows[0].Cells[1]!.Style].ClassName, Is.EqualTo(Right), "a number aligns right in an XLS too");
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
            Assert.That((sheets[0].RowCount, sheets[0].Columns.Count), Is.EqualTo((6, 5)), "the empty rows and columns after the last value are not counted");
            Assert.That((sheets[1].Rows.Count, sheets[1].RowCount, sheets[1].Columns.Count), Is.EqualTo((0, 0, 0)), "a sheet of empty cells is empty");
        }
    }

    [Test]
    public void AnOdsSheetTakesItsWidthsHeightsHiddenPartsAndMerges()
    {
        var sheets = WorkbookReader.ReadOrDefault(OdsArchive(OdsLayout), 10)!;
        var sheet = sheets.Single();
        var cells = Cells(sheet);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sheet.Name, Is.EqualTo("Viditelný"), "a sheet its application does not display is left out");
            Assert.That(sheet.Columns.Select(static column => (column.Index, column.Width)), Is.EqualTo([(0, 96), (2, 64), (3, 64)]), "a collapsed column is left out, a styled one takes its width");
            Assert.That(sheet.Rows.Select(static row => (row.Index, row.Height)), Is.EqualTo(new (int, int?)[] { (0, 40), (2, null) }), "a collapsed row is left out, a styled one takes its height");
            Assert.That(cells[0], Is.EqualTo(["Spojeno:2x1", "-", "7:1x1"]), "a spanned cell covers its shown columns and rows");
            Assert.That(cells[1], Is.EqualTo(["PRAVDA:1x1", ":1x1", ":1x1"]));
            Assert.That(sheet.Rows.Select(row => sheet.Styles[row.Cells[^1]!.Style].ClassName), Is.EqualTo([Right, ""]), "a number aligns right");
            Assert.That(sheet.Styles[sheet.Rows[1].Cells[0]!.Style].ClassName, Is.EqualTo(Center), "a logical value aligns in the centre");
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

    // The covered cells of a merge left out.
    private static IEnumerable<string> Lines(SheetPreview sheet)
    {
        return sheet.Rows.Select(static row => string.Join('|', row.Cells.OfType<SheetCell>().Select(static cell => cell.Text)));
    }

    // Each shown cell as its text and spans, a covered one as -.
    private static List<string[]> Cells(SheetPreview sheet)
    {
        return [.. sheet.Rows.Select(static row => row.Cells.Select(static cell => cell is null ? "-" : string.Create(CultureInfo.InvariantCulture, $"{cell.Text}:{cell.ColumnSpan}x{cell.RowSpan}")).ToArray())];
    }

    private static int[] Channels(string rgb)
    {
        return [Convert.ToInt32(rgb[..2], 16), Convert.ToInt32(rgb[2..4], 16), Convert.ToInt32(rgb[4..], 16)];
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

    // Each sheet is the content of its worksheet element; every workbook carries the styles and the theme above.
    private static byte[] Xlsx(params (string Name, SheetStateValues? State, string Worksheet)[] sheets)
    {
        using var stream = new MemoryStream();

        using (var document = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = document.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new Sheets());

            Feed(workbookPart.AddNewPart<WorkbookStylesPart>(), Styles);
            Feed(workbookPart.AddNewPart<ThemePart>(), Theme);

            uint sheetId = 1;

            foreach (var (name, state, worksheet) in sheets)
            {
                var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();

                Feed(worksheetPart, $"""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">{worksheet}</worksheet>""");
                workbookPart.Workbook.Sheets!.Append(new Sheet { Id = workbookPart.GetIdOfPart(worksheetPart), SheetId = sheetId++, Name = name, State = state });
            }
        }

        return stream.ToArray();
    }

    private static void Feed(OpenXmlPart part, string xml)
    {
        using var content = new MemoryStream(Encoding.UTF8.GetBytes(xml));

        part.FeedData(content);
    }
}

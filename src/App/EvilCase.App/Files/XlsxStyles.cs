using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The cell styles of an XLSX workbook: fonts, solid fills, borders, alignment, wrapping and indent by each cell's
/// format. Conditional formats are not read.
/// </summary>
internal sealed class XlsxStyles : IDisposable
{
    private const string Bold = "ec-preview-sheet-bold";

    private const string Italic = "ec-preview-sheet-italic";

    private const string Underline = "ec-preview-sheet-underline";

    private const string Strike = "ec-preview-sheet-strike";

    private const string Wrap = "ec-preview-sheet-wrap";

    private const string Black = "#000000";

    // Excel's colours by index before a workbook's own palette replaces them; 64 and 65 are the automatic ones.
    private static readonly string[] DefaultPalette =
    [
        "000000", "FFFFFF", "FF0000", "00FF00", "0000FF", "FFFF00", "FF00FF", "00FFFF",
        "000000", "FFFFFF", "FF0000", "00FF00", "0000FF", "FFFF00", "FF00FF", "00FFFF",
        "800000", "008000", "000080", "808000", "800080", "008080", "C0C0C0", "808080",
        "9999FF", "993366", "FFFFCC", "CCFFFF", "660066", "FF8080", "0066CC", "CCCCFF",
        "000080", "FF00FF", "FFFF00", "00FFFF", "800080", "800000", "008080", "0000FF",
        "00CCFF", "CCFFFF", "CCFFCC", "FFFF99", "99CCFF", "FF99CC", "CC99FF", "FFCC99",
        "3366FF", "33CCCC", "99CC00", "FFCC00", "FF9900", "FF6600", "666699", "969696",
        "003366", "339966", "003300", "333300", "993300", "993366", "333399", "333333",
    ];

    private static readonly XlsxCellFormat NoFormat = new(ClassName: "", Horizontal: null, Css: "", Fill: null, Left: null, Right: null, Top: null, Bottom: null);

    private readonly SpreadsheetDocument document;

    private readonly List<XlsxCellFormat> formats;

    private XlsxStyles(SpreadsheetDocument document, List<XlsxCellFormat> formats)
    {
        this.document = document;
        this.formats = formats;
    }

    /// <summary>
    /// The styles of the workbook, or <see langword="null"/> where it is no XLSX package the styles can be read from.
    /// </summary>
    public static XlsxStyles? OpenOrDefault(byte[] content)
    {
        SpreadsheetDocument? package = null;

        try
        {
            package = SpreadsheetDocument.Open(new MemoryStream(content, writable: false), isEditable: false);

            return new XlsxStyles(package, CellFormats(package.WorkbookPart!));
        }
        catch (Exception)
        {
            // A workbook the Excel reader reads without its styles, such as an XLSB.
            package?.Dispose();

            return null;
        }
    }

    public void Dispose()
    {
        this.document.Dispose();
    }

    /// <summary>
    /// The style of each drawn cell of the sheet and the height of each row in pixels, 0 where hidden, its first
    /// <paramref name="rowCount"/> rows read, or <see langword="null"/> where the sheet's formats cannot be read. The
    /// Excel reader gives the first of the rows a sheet leaves out the height of the row before it.
    /// </summary>
    public (Func<SheetRegion, string, SheetStyle> Style, Func<int, int> RowHeight)? Sheet(string name, int rowCount)
    {
        try
        {
            var workbook = this.document.WorkbookPart!;
            var sheet = workbook.Workbook?.Sheets?.Elements<Sheet>().FirstOrDefault(sheet => string.Equals(sheet.Name?.Value, name, StringComparison.Ordinal));

            if (sheet?.Id?.Value is not { } partId || workbook.GetPartById(partId) is not WorksheetPart worksheet)
                return null;

            var (formatAt, rowHeight) = this.ReadWorksheet(worksheet, rowCount);

            return ((region, align) => Style(region, align, formatAt), rowHeight);
        }
        catch (Exception)
        {
            // The sheet shows without its styles.
            return null;
        }
    }

    // A drawn cell draws the borders and grid lines it shares with the cells after it; its own left and top borders only
    // in the first shown column and row. A fill on either side hides a grid line.
    private static SheetStyle Style(SheetRegion region, string align, Func<int, int, XlsxCellFormat> formatAt)
    {
        var format = formatAt(region.Row, region.Column);
        var after = region.NextColumn >= 0 ? formatAt(region.Row, region.NextColumn) : NoFormat;
        var below = region.NextRow >= 0 ? formatAt(region.NextRow, region.Column) : NoFormat;
        var right = formatAt(region.Row, region.LastColumn).Right ?? after.Left;
        var bottom = formatAt(region.LastRow, region.Column).Bottom ?? below.Top;

        var declarations = new List<string>(6);

        if (format.Css.Length > 0)
            declarations.Add(format.Css);

        if (region.FirstColumn && format.Left is { } left)
            declarations.Add("border-left:" + left);

        if (region.FirstRow && format.Top is { } top)
            declarations.Add("border-top:" + top);

        if (right is not null)
            declarations.Add("border-right:" + right);
        else if ((format.Fill ?? after.Fill) is { } fill)
            declarations.Add("border-right-color:" + fill);

        if (bottom is not null)
            declarations.Add("border-bottom:" + bottom);
        else if ((format.Fill ?? below.Fill) is { } fill)
            declarations.Add("border-bottom-color:" + fill);

        return new SheetStyle(
            WorkbookReader.ClassNames(format.Horizontal ?? align, format.ClassName),
            declarations.Count > 0 ? string.Join(';', declarations) : null);
    }

    private static List<XlsxCellFormat> CellFormats(WorkbookPart workbook)
    {
        var stylesheet = workbook.WorkbookStylesPart?.Stylesheet;

        if (stylesheet?.CellFormats is not { } cellFormats)
            return [];

        var theme = ThemeColors(workbook.ThemePart);
        var palette = Palette(stylesheet.Colors?.IndexedColors);
        var fonts = stylesheet.Fonts?.Elements<Font>().ToList() ?? [];
        var fills = stylesheet.Fills?.Elements<Fill>().ToList() ?? [];
        var borders = stylesheet.Borders?.Elements<Border>().ToList() ?? [];
        var baseSize = fonts.FirstOrDefault()?.FontSize?.Val?.Value ?? 11;

        string? ColorOf(ColorType? color)
        {
            return Color(color, theme, palette);
        }

        string? BorderOf(BorderPropertiesType? side)
        {
            return BorderSide(side?.Style?.InnerText, ColorOf(side?.Color) ?? Black);
        }

        return
        [
            .. cellFormats.Elements<CellFormat>().Select(format =>
            {
                var font = Item(fonts, format.FontId?.Value);
                var fill = Item(fills, format.FillId?.Value)?.PatternFill;
                var border = Item(borders, format.BorderId?.Value);
                var fillColor = string.Equals(fill?.PatternType?.InnerText, "solid", StringComparison.Ordinal) ? ColorOf(fill?.ForegroundColor) : null;
                var (className, css) = FontAndAlignment(font, ColorOf(font?.Color), baseSize, format.Alignment);

                if (fillColor is not null)
                    css.Add("background-color:" + fillColor);

                return new XlsxCellFormat(
                    className,
                    HorizontalClass(format.Alignment?.Horizontal?.InnerText),
                    string.Join(';', css),
                    fillColor,
                    BorderOf((BorderPropertiesType?)border?.LeftBorder ?? border?.StartBorder),
                    BorderOf((BorderPropertiesType?)border?.RightBorder ?? border?.EndBorder),
                    BorderOf(border?.TopBorder),
                    BorderOf(border?.BottomBorder));
            }),
        ];
    }

    // The classes of a font, vertical alignment and wrapping, and the declarations of the font's colour other than
    // black, its size relative to the workbook's default font, and the indent.
    private static (string ClassName, List<string> Css) FontAndAlignment(Font? font, string? color, double baseSize, Alignment? alignment)
    {
        var css = new List<string>(4);

        if (color is not null && !string.Equals(color, Black, StringComparison.Ordinal))
            css.Add("color:" + color);

        if (font?.FontSize?.Val?.Value is { } size && Math.Abs(size - baseSize) > 0.001)
            css.Add(string.Create(CultureInfo.InvariantCulture, $"font-size:{size / baseSize:0.###}em"));

        if (alignment?.Indent?.Value is > 0 and var indent)
            css.Add(WorkbookReader.Indent((int)indent));

        var underlined = font?.Underline is { } underline && !string.Equals(underline.Val?.InnerText, "none", StringComparison.Ordinal);

        var className = WorkbookReader.ClassNames(
            VerticalClass(alignment?.Vertical?.InnerText),
            alignment?.WrapText?.Value == true ? Wrap : "",
            IsOn(font?.Bold) ? Bold : "",
            IsOn(font?.Italic) ? Italic : "",
            underlined ? Underline : "",
            IsOn(font?.Strike) ? Strike : "");

        return (className, css);
    }

    // The colours Excel numbers 0 to 11: the light and dark ones of the theme part swap places.
    private static List<string?> ThemeColors(ThemePart? themePart)
    {
        if (themePart?.Theme?.ThemeElements?.ColorScheme is not { } scheme)
            return [];

        var colors = scheme.Elements()
            .Where(static element => element is not DocumentFormat.OpenXml.Drawing.ExtensionList)
            .Select(static element => element.GetFirstChild<DocumentFormat.OpenXml.Drawing.RgbColorModelHex>()?.Val?.Value ?? element.GetFirstChild<DocumentFormat.OpenXml.Drawing.SystemColor>()?.LastColor?.Value)
            .ToList();

        if (colors.Count >= 4)
            (colors[0], colors[1], colors[2], colors[3]) = (colors[1], colors[0], colors[3], colors[2]);

        return colors;
    }

    private static string?[] Palette(IndexedColors? indexedColors)
    {
        var palette = DefaultPalette.ToArray<string?>();
        var index = 0;

        foreach (var color in indexedColors?.Elements<RgbColor>() ?? [])
        {
            if (index < palette.Length)
                palette[index] = color.Rgb?.Value is { Length: >= 6 } argb ? argb[^6..] : null;

            index++;
        }

        return palette;
    }

    // A colour as #RRGGBB with its tint, or null where it is automatic or not known.
    private static string? Color(ColorType? color, List<string?> theme, string?[] palette)
    {
        if (color is null || color.Auto?.Value == true)
            return null;

        string? rgb = null;

        if (color.Rgb?.Value is { Length: >= 6 } argb)
            rgb = argb[^6..];
        else if (color.Theme?.Value is { } themeIndex)
            rgb = Item(theme, themeIndex);
        else if (color.Indexed?.Value is { } paletteIndex)
            rgb = palette.ElementAtOrDefault((int)paletteIndex);

        if (rgb is not { Length: 6 } || !int.TryParse(rgb, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
            return null;

        return "#" + (color.Tint?.Value is { } tint and not 0 ? Tinted(value, tint) : value).ToString("X6", CultureInfo.InvariantCulture);
    }

    // Excel's tint moves the lightness towards black below zero and towards white above it.
    private static int Tinted(int rgb, double tint)
    {
        var red = ((rgb >> 16) & 0xFF) / 255.0;
        var green = ((rgb >> 8) & 0xFF) / 255.0;
        var blue = (rgb & 0xFF) / 255.0;

        var max = Math.Max(red, Math.Max(green, blue));
        var min = Math.Min(red, Math.Min(green, blue));
        var chroma = max - min;
        var lightness = (max + min) / 2;
        var saturation = chroma == 0 ? 0 : chroma / (1 - Math.Abs((2 * lightness) - 1));
        var hue = 0.0;

        if (chroma > 0 && max == red)
            hue = (((green - blue) / chroma) + 6) % 6;
        else if (chroma > 0 && max == green)
            hue = ((blue - red) / chroma) + 2;
        else if (chroma > 0)
            hue = ((red - green) / chroma) + 4;

        lightness = tint < 0 ? lightness * (1 + tint) : (lightness * (1 - tint)) + tint;
        chroma = (1 - Math.Abs((2 * lightness) - 1)) * saturation;

        var second = chroma * (1 - Math.Abs((hue % 2) - 1));
        var (r, g, b) = (int)hue switch
        {
            0 => (chroma, second, 0.0),
            1 => (second, chroma, 0.0),
            2 => (0.0, chroma, second),
            3 => (0.0, second, chroma),
            4 => (second, 0.0, chroma),
            _ => (chroma, 0.0, second),
        };
        var match = lightness - (chroma / 2);

        static int Channel(double value)
        {
            return (int)Math.Round(Math.Clamp(value, 0, 1) * 255, MidpointRounding.AwayFromZero);
        }

        return (Channel(r + match) << 16) | (Channel(g + match) << 8) | Channel(b + match);
    }

    // A border side as CSS, or null where the side has none.
    private static string? BorderSide(string? style, string color)
    {
        var line = style switch
        {
            "thin" => "1px solid",
            "medium" => "2px solid",
            "thick" => "3px solid",
            "dashed" or "dashDot" or "dashDotDot" => "1px dashed",
            "mediumDashed" or "mediumDashDot" or "mediumDashDotDot" or "slantDashDot" => "2px dashed",
            "dotted" or "hair" => "1px dotted",
            "double" => "3px double",
            _ => null,
        };

        return line is null ? null : line + " " + color;
    }

    // An explicit horizontal alignment, null for the general one that follows the value.
    private static string? HorizontalClass(string? horizontal)
    {
        return horizontal switch
        {
            "center" or "centerContinuous" => WorkbookReader.AlignCenter,
            "right" => WorkbookReader.AlignRight,
            "justify" or "distributed" => WorkbookReader.AlignJustify,
            "left" or "fill" => "",
            _ => null,
        };
    }

    private static string VerticalClass(string? vertical)
    {
        return vertical switch
        {
            "top" or "justify" or "distributed" => WorkbookReader.AlignTop,
            "center" => WorkbookReader.AlignMiddle,
            _ => "",
        };
    }

    private static bool IsOn(BooleanPropertyType? property)
    {
        return property is not null && (property.Val?.Value ?? true);
    }

    private static T? Item<T>(List<T> items, uint? index)
    {
        return index is { } position && position < items.Count ? items[(int)position] : default;
    }

    private static string? Attribute(OpenXmlReader reader, string localName)
    {
        foreach (var attribute in reader.Attributes)
        {
            if (string.Equals(attribute.LocalName, localName, StringComparison.Ordinal))
                return attribute.Value;
        }

        return null;
    }

    private static int ParseIndex(string value)
    {
        return int.Parse(value, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    // The zero-based column of a reference such as AB12.
    private static int ColumnOf(string reference)
    {
        var column = 0;

        foreach (var letter in reference.TakeWhile(char.IsAsciiLetter))
            column = (column * 26) + char.ToUpperInvariant(letter) - 'A' + 1;

        return column - 1;
    }

    private static bool IsTrue(string? value)
    {
        return value is "1" or "true";
    }

    // The format of each position of the read area, the cell's own, else its row's, else its column's; and the height
    // of each row, its own, else the sheet's default.
    private (Func<int, int, XlsxCellFormat> FormatAt, Func<int, int> RowHeight) ReadWorksheet(WorksheetPart worksheet, int rowCount)
    {
        var cells = new Dictionary<(int Row, int Column), int>();
        var rows = new Dictionary<int, int>();
        var heights = new Dictionary<int, int>();
        var columns = new int[WorkbookReader.MaxColumns];
        var defaultHeight = WorkbookReader.Pixels(15);
        var row = -1;
        var column = -1;

        using (var reader = OpenXmlReader.Create(worksheet))
        {
            while (reader.Read() && row < rowCount)
            {
                if (!reader.IsStartElement)
                    continue;

                if (reader.ElementType == typeof(SheetFormatProperties) && Attribute(reader, "defaultRowHeight") is { } points)
                {
                    defaultHeight = WorkbookReader.Pixels(double.Parse(points, CultureInfo.InvariantCulture));
                }
                else if (reader.ElementType == typeof(Column))
                {
                    var max = Math.Min(ParseIndex(Attribute(reader, "max") ?? "1"), WorkbookReader.MaxColumns);

                    for (var index = ParseIndex(Attribute(reader, "min") ?? "1") - 1; index < max; index++)
                        columns[index] = ParseIndex(Attribute(reader, "style") ?? "0");
                }
                else if (reader.ElementType == typeof(Row))
                {
                    row = Attribute(reader, "r") is { } reference ? ParseIndex(reference) - 1 : row + 1;
                    column = -1;

                    if (IsTrue(Attribute(reader, "customFormat")) && Attribute(reader, "s") is { } rowStyle)
                        rows[row] = ParseIndex(rowStyle);

                    if (IsTrue(Attribute(reader, "hidden")))
                        heights[row] = 0;
                    else if (Attribute(reader, "ht") is { } height)
                        heights[row] = WorkbookReader.Pixels(double.Parse(height, CultureInfo.InvariantCulture));
                }
                else if (reader.ElementType == typeof(Cell))
                {
                    column = Attribute(reader, "r") is { } reference ? ColumnOf(reference) : column + 1;

                    // A cell without a style takes the default one, not its row's or column's.
                    if (column < WorkbookReader.MaxColumns)
                        cells[(row, column)] = ParseIndex(Attribute(reader, "s") ?? "0");
                }
            }
        }

        return (
            (atRow, atColumn) =>
            {
                if (!cells.TryGetValue((atRow, atColumn), out var index) && !rows.TryGetValue(atRow, out index))
                    index = columns[atColumn];

                return index < this.formats.Count ? this.formats[index] : NoFormat;
            },
            atRow => heights.GetValueOrDefault(atRow, defaultHeight));
    }
}

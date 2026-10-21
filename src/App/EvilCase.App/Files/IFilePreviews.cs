using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The previews of the formats the browser cannot show itself, parsed by a library the browser loads on first use.
/// A file the library cannot parse throws a <c>JSException</c>.
/// </summary>
internal interface IFilePreviews
{
    /// <summary>
    /// Converts a HEIC or HEIF picture to PNG and returns the blob URL the browser shows it by.
    /// </summary>
    public Task<string> CreatePngUrl(Stream content, CancellationToken token);

    /// <summary>
    /// Renders a DOCX document as pages into the element.
    /// </summary>
    public Task RenderDocument(ElementReference host, Stream content, CancellationToken token);

    /// <summary>
    /// Shows the PDF at <paramref name="url"/> in the element, fitted to its width, and returns its page count. The
    /// view then hears of the page, the zoom and the search matches as they change. A file the library cannot open, a
    /// password-protected one included, throws a <c>JSException</c>.
    /// </summary>
    public Task<int> RenderPdf(ElementReference host, string url, DotNetObjectReference<PdfPreview> view, CancellationToken token);

    public Task ZoomPdf(ElementReference host, PdfZoom zoom);

    public Task GoToPdfPage(ElementReference host, int page);

    /// <summary>
    /// Highlights every match of <paramref name="query"/> in the PDF and scrolls to the one <paramref name="find"/>
    /// names; an empty query clears the highlights.
    /// </summary>
    public Task FindInPdf(ElementReference host, string query, PdfFind find);

    /// <summary>
    /// Releases the PDF shown in the element: its document, worker, canvases and listeners.
    /// </summary>
    public Task DisposePdf(ElementReference host);

    /// <summary>
    /// Reads every sheet of a workbook, no more than <paramref name="maxRows"/> rows of each.
    /// </summary>
    public Task<IReadOnlyList<SheetPreview>> ReadWorkbook(Stream content, int maxRows, CancellationToken token);

    public Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token);
}

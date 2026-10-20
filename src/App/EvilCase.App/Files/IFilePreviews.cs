using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The previews of the formats the browser cannot show itself, parsed by a library the browser loads on first use.
/// A file the library cannot parse throws a <c>JSException</c>.
/// </summary>
internal interface IFilePreviews
{
    /// <summary>
    /// Converts a HEIC or TIFF picture to PNG and returns the blob URL the browser shows it by.
    /// </summary>
    public Task<string> CreatePngUrl(string fileName, Stream content, CancellationToken token);

    /// <summary>
    /// Renders a DOCX document as pages into the element.
    /// </summary>
    public Task RenderDocument(ElementReference host, Stream content, CancellationToken token);

    /// <summary>
    /// Draws the pages of the PDF at <paramref name="url"/> into the element and returns their count. A file the
    /// library cannot open, a password-protected one included, throws a <c>JSException</c>.
    /// </summary>
    public Task<int> RenderPdf(ElementReference host, string url, CancellationToken token);

    /// <summary>
    /// Releases the PDF drawn into the element: its document, worker and canvases.
    /// </summary>
    public Task DisposePdf(ElementReference host);

    /// <summary>
    /// Reads every sheet of a workbook, no more than <paramref name="maxRows"/> rows of each.
    /// </summary>
    public Task<IReadOnlyList<SheetPreview>> ReadWorkbook(Stream content, int maxRows, CancellationToken token);

    public Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token);
}

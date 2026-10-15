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
    /// Reads every sheet of a workbook, no more than <paramref name="maxRows"/> rows of each.
    /// </summary>
    public Task<IReadOnlyList<SheetPreview>> ReadWorkbook(Stream content, int maxRows, CancellationToken token);

    public Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token);
}

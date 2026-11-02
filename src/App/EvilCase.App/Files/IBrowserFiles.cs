using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.App.Files;

/// <summary>
/// The browser's side of files: downloads, blob URLs, the file picker and the drop zone.
/// </summary>
internal interface IBrowserFiles
{
    /// <summary>
    /// Has the browser download the file by an ordinary request to its content address.
    /// </summary>
    public Task DownloadFile(Guid fileId, int? version, string fileName, CancellationToken token);

    /// <summary>
    /// Has the browser download an archive by an ordinary request to its address, under the name the server gives.
    /// </summary>
    public Task DownloadArchive(string archiveUrl, CancellationToken token);

    /// <summary>
    /// Has the browser save content the app holds in memory under the name, never shown inline.
    /// </summary>
    public Task DownloadContent(string fileName, Stream content, CancellationToken token);

    /// <summary>
    /// Wraps the bytes in a blob of the given type and returns the URL the browser shows it by.
    /// </summary>
    public Task<string> CreateBlobUrl(string mediaType, Stream content, CancellationToken token);

    public Task RevokeBlobUrl(string url);

    /// <summary>
    /// Opens the browser's file picker of a hidden input from a control that cannot be its label.
    /// </summary>
    public Task PickFile(ElementReference input, CancellationToken token);

    /// <summary>
    /// Makes the card a drop zone that hands dropped files to its first file input.
    /// </summary>
    public Task AttachDropZone(ElementReference card, CancellationToken token);
}

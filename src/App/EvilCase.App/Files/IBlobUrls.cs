namespace EvilBrains.EvilCase.App.Files;

internal interface IBlobUrls
{
    /// <summary>
    /// Wraps the bytes in a blob of the given type and returns the URL the browser shows it by.
    /// </summary>
    public Task<string> CreateBlobUrl(string mediaType, Stream content, CancellationToken token);

    public Task RevokeBlobUrl(string url);
}

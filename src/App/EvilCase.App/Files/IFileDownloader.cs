namespace EvilBrains.EvilCase.App.Files;

internal interface IFileDownloader
{
    /// <summary>
    /// Has the browser download the file by an ordinary request to its content address.
    /// </summary>
    public Task DownloadFile(Guid fileId, string fileName, CancellationToken token);

    /// <summary>
    /// Has the browser save content the app holds in memory under the name, never shown inline.
    /// </summary>
    public Task DownloadContent(string fileName, Stream content, CancellationToken token);
}

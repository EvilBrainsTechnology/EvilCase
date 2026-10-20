namespace EvilBrains.EvilCase.App.Files;

internal interface IFileDownloader
{
    /// <summary>
    /// Has the browser download the file by an ordinary request to its content address.
    /// </summary>
    public Task DownloadFile(Guid fileId, string fileName, CancellationToken token);
}

namespace EvilBrains.EvilCase.App.Files;

internal interface IFileDownloader
{
    public Task SaveFile(string fileName, FileContent content, CancellationToken token);
}

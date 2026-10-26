using EvilBrains.EvilCase.App.Files;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A render test never reaches the browser; this internal interface cannot be substituted with
/// NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a plain recorder stands in
/// instead.
/// </summary>
internal sealed class StubFileDownloader : IFileDownloader
{
    public List<(Guid FileId, string FileName)> Downloaded { get; } = [];

    public List<(string FileName, byte[] Content)> DownloadedContents { get; } = [];

    public Task DownloadFile(Guid fileId, string fileName, CancellationToken token)
    {
        this.Downloaded.Add((fileId, fileName));

        return Task.CompletedTask;
    }

    public async Task DownloadContent(string fileName, Stream content, CancellationToken token)
    {
        await using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);
        this.DownloadedContents.Add((fileName, bytes.ToArray()));
    }
}

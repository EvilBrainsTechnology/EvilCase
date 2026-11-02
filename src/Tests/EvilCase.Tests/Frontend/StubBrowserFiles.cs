using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// A render test never reaches the browser; this internal interface cannot be substituted with
/// NSubstitute without a DynamicProxyGenAssembly2 visibility grant, so a plain recorder stands in
/// instead.
/// </summary>
internal sealed class StubBrowserFiles : IBrowserFiles
{
    public List<(Guid FileId, int? Version, string FileName)> Downloaded { get; } = [];

    public List<(string FileName, byte[] Content)> DownloadedContents { get; } = [];

    public List<(string MediaType, string Url)> Created { get; } = [];

    public List<string> Revoked { get; } = [];

    public Task DownloadFile(Guid fileId, int? version, string fileName, CancellationToken token)
    {
        this.Downloaded.Add((fileId, version, fileName));

        return Task.CompletedTask;
    }

    public Task DownloadArchive(string archiveUrl, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public async Task DownloadContent(string fileName, Stream content, CancellationToken token)
    {
        await using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);
        this.DownloadedContents.Add((fileName, bytes.ToArray()));
    }

    public Task<string> CreateBlobUrl(string mediaType, Stream content, CancellationToken token)
    {
        var url = $"blob:test/{this.Created.Count}";
        this.Created.Add((mediaType, url));

        return Task.FromResult(url);
    }

    public Task RevokeBlobUrl(string url)
    {
        this.Revoked.Add(url);

        return Task.CompletedTask;
    }

    public Task PickFile(ElementReference input, CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public Task AttachDropZone(ElementReference card, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

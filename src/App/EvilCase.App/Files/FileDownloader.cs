using EvilBrains.EvilCase.App.Auth;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FileDownloader(IJSRuntime jsRuntime, IAuthSession session) : IFileDownloader, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task DownloadFile(Guid fileId, int? version, string fileName, CancellationToken token)
    {
        var files = await this.Module(token);

        await session.EnsureFresh(token);

        await files.InvokeVoidAsync("downloadFile", token, FileUrl.Content(fileId, version), fileName);
    }

    public async Task DownloadArchive(string archiveUrl, CancellationToken token)
    {
        var files = await this.Module(token);

        await session.EnsureFresh(token);

        await files.InvokeVoidAsync("downloadFile", token, archiveUrl, "");
    }

    public async Task DownloadContent(string fileName, Stream content, CancellationToken token)
    {
        var files = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        await files.InvokeVoidAsync("downloadContent", token, fileName, streamRef);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    private async Task<IJSObjectReference> Module(CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/files.js");

        return this.module;
    }
}

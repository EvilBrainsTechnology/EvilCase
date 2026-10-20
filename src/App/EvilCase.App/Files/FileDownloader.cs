using EvilBrains.EvilCase.App.Auth;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FileDownloader(IJSRuntime jsRuntime, IAuthSession session) : IFileDownloader, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task DownloadFile(Guid fileId, string fileName, CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/files.js");

        await session.EnsureFresh(token);

        await this.module.InvokeVoidAsync("downloadFile", token, FileUrl.Content(fileId), fileName);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }
}

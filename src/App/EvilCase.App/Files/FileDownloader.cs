using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FileDownloader(IJSRuntime jsRuntime) : IFileDownloader, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task SaveFile(string fileName, FileContent content, CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/files.js");

        using var streamRef = new DotNetStreamReference(content.Content);

        await this.module.InvokeVoidAsync("downloadBlob", token, fileName, content.MediaType, streamRef);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }
}

using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class BlobUrls(IJSRuntime jsRuntime) : IBlobUrls, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task<string> CreateBlobUrl(string mediaType, Stream content, CancellationToken token)
    {
        var files = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        return await files.InvokeAsync<string>("createObjectUrl", token, mediaType, streamRef);
    }

    public async Task RevokeBlobUrl(string url)
    {
        var files = await this.Module(CancellationToken.None);

        await files.InvokeVoidAsync("revokeObjectUrl", url);
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

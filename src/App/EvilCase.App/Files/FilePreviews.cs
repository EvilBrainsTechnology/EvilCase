using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FilePreviews(IJSRuntime jsRuntime) : IFilePreviews, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task<string> CreatePngUrl(string fileName, Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        // Never cancelled once started: a URL the browser made after a cancel must reach the caller to be revoked.
        return await previews.InvokeAsync<string>("createPngUrl", CancellationToken.None, fileName, streamRef);
    }

    public async Task RenderDocument(ElementReference host, Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        await previews.InvokeVoidAsync("renderDocument", token, host, streamRef);
    }

    public async Task<IReadOnlyList<SheetPreview>> ReadWorkbook(Stream content, int maxRows, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        return await previews.InvokeAsync<IReadOnlyList<SheetPreview>>("readWorkbook", token, streamRef, maxRows);
    }

    public async Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        return await previews.InvokeAsync<EmailPreview>("readEmail", token, fileName, streamRef);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    private async Task<IJSObjectReference> Module(CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/file-preview.js");

        return this.module;
    }
}

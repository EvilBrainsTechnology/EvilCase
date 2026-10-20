using System.Diagnostics;
using EvilBrains.EvilCase.App.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FilePreviews(IJSRuntime jsRuntime) : IFilePreviews, IAsyncDisposable
{
    private IJSObjectReference? module;

    public async Task<string> CreatePngUrl(Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        // Never cancelled once started: a URL the browser made after a cancel must reach the caller to be revoked.
        return await previews.InvokeAsync<string>("createPngUrl", CancellationToken.None, streamRef);
    }

    public async Task RenderDocument(ElementReference host, Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        await previews.InvokeVoidAsync("renderDocument", token, host, streamRef);
    }

    public async Task<int> RenderPdf(ElementReference host, string url, DotNetObjectReference<PdfPreview> view, CancellationToken token)
    {
        var previews = await this.Module(token);

        return await previews.InvokeAsync<int>("renderPdf", token, host, url, view);
    }

    public async Task ZoomPdf(ElementReference host, PdfZoom zoom)
    {
        var previews = await this.Module(CancellationToken.None);

        var value = zoom switch
        {
            PdfZoom.In => "in",
            PdfZoom.Out => "out",
            PdfZoom.PageWidth => "page-width",
            PdfZoom.PageFit => "page-fit",
            _ => throw new UnreachableException(),
        };

        await previews.InvokeVoidAsync("zoomPdf", host, value);
    }

    public async Task GoToPdfPage(ElementReference host, int page)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("goToPdfPage", host, page);
    }

    public async Task FindInPdf(ElementReference host, string query, PdfFind find)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("findInPdf", host, query, find != PdfFind.New, find == PdfFind.Previous);
    }

    public async Task DisposePdf(ElementReference host)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("disposePdf", host);
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

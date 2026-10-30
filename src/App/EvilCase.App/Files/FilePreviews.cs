using System.Diagnostics;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
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

    public async Task<int> RenderDocument(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        return await previews.InvokeAsync<int>("renderDocument", token, host, streamRef, view);
    }

    public async Task OpenTextView(ElementReference stage, DotNetObjectReference<PreviewToolbar> view)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("openTextView", stage, view);
    }

    public async Task ZoomView(ElementReference element, PreviewZoom zoom)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("zoomView", element, ZoomValue(zoom));
    }

    public async Task GoToViewPage(ElementReference element, int page)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("goToViewPage", element, page);
    }

    public async Task FindInView(ElementReference element, string query, PreviewFind find)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("findInView", element, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task DisposeView(ElementReference element)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("disposeView", element);
    }

    public async Task<int> RenderPdf(ElementReference host, string url, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        var previews = await this.Module(token);

        return await previews.InvokeAsync<int>("renderPdf", token, host, url, view);
    }

    public async Task<int> RenderPdf(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        return await previews.InvokeAsync<int>("renderPdfContent", token, host, streamRef, view);
    }

    public async Task ZoomPdf(ElementReference host, PreviewZoom zoom)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("zoomPdf", host, ZoomValue(zoom));
    }

    public async Task GoToPdfPage(ElementReference host, int page)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("goToPdfPage", host, page);
    }

    public async Task FindInPdf(ElementReference host, string query, PreviewFind find)
    {
        var previews = await this.Module(CancellationToken.None);

        await previews.InvokeVoidAsync("findInPdf", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
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

    public async Task<string?> DetectExtension(Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        return await previews.InvokeAsync<string?>("detectExtension", token, streamRef);
    }

    public async ValueTask DisposeAsync()
    {
        if (this.module is not null)
            await this.module.DisposeAsync();
    }

    internal static string LanguageValue(CodeLanguage language)
    {
        return language switch
        {
            CodeLanguage.Json => "json",
            CodeLanguage.Xml => "xml",
            CodeLanguage.Yaml => "yaml",
            CodeLanguage.Html => "html",
            CodeLanguage.Markdown => "markdown",
            _ => throw new UnreachableException(),
        };
    }

    internal static string ZoomValue(PreviewZoom zoom)
    {
        return zoom switch
        {
            PreviewZoom.In => "in",
            PreviewZoom.Out => "out",
            PreviewZoom.PageWidth => "page-width",
            PreviewZoom.PageFit => "page-fit",
            PreviewZoom.Actual => "actual",
            _ => throw new UnreachableException(),
        };
    }

    private async Task<IJSObjectReference> Module(CancellationToken token)
    {
        this.module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", token, "./js/file-preview.js");

        return this.module;
    }
}

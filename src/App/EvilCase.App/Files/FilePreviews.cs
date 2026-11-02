using System.Diagnostics;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.App.Files;

internal sealed class FilePreviews(IJSRuntime jsRuntime) : JsModule(jsRuntime, "./js/file-preview.js"), IFilePreviews
{
    public async Task<string> CreatePngUrl(Stream content, CancellationToken token)
    {
        var previews = await this.Module(token);

        using var streamRef = new DotNetStreamReference(content);

        // Never cancelled once started: a URL the browser made after a cancel must reach the caller to be revoked.
        return await previews.InvokeAsync<string>("createPngUrl", CancellationToken.None, streamRef);
    }

    public async Task<int> RenderDocument(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        using var streamRef = new DotNetStreamReference(content);

        return await this.Invoke<int>("renderDocument", token, host, streamRef, view);
    }

    public async Task OpenTextView(ElementReference stage, DotNetObjectReference<PreviewToolbar> view)
    {
        await this.InvokeVoid("openTextView", stage, view);
    }

    public async Task ZoomView(ElementReference element, PreviewZoom zoom)
    {
        await this.InvokeVoid("zoomView", element, ZoomValue(zoom));
    }

    public async Task GoToViewPage(ElementReference element, int page)
    {
        await this.InvokeVoid("goToViewPage", element, page);
    }

    public async Task FindInView(ElementReference element, string query, PreviewFind find)
    {
        await this.InvokeVoid("findInView", element, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task DisposeView(ElementReference element)
    {
        await this.InvokeVoid("disposeView", element);
    }

    public async Task<int> RenderPdf(ElementReference host, string url, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        return await this.Invoke<int>("renderPdf", token, host, url, view);
    }

    public async Task<int> RenderPdf(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        using var streamRef = new DotNetStreamReference(content);

        return await this.Invoke<int>("renderPdfContent", token, host, streamRef, view);
    }

    public async Task ZoomPdf(ElementReference host, PreviewZoom zoom)
    {
        await this.InvokeVoid("zoomPdf", host, ZoomValue(zoom));
    }

    public async Task GoToPdfPage(ElementReference host, int page)
    {
        await this.InvokeVoid("goToPdfPage", host, page);
    }

    public async Task FindInPdf(ElementReference host, string query, PreviewFind find)
    {
        await this.InvokeVoid("findInPdf", host, query, find != PreviewFind.New, find == PreviewFind.Previous);
    }

    public async Task DisposePdf(ElementReference host)
    {
        await this.InvokeVoid("disposePdf", host);
    }

    public async Task<IReadOnlyList<SheetPreview>> ReadWorkbook(Stream content, int maxRows, CancellationToken token)
    {
        using var streamRef = new DotNetStreamReference(content);

        return await this.Invoke<IReadOnlyList<SheetPreview>>("readWorkbook", token, streamRef, maxRows);
    }

    public async Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token)
    {
        using var streamRef = new DotNetStreamReference(content);

        return await this.Invoke<EmailPreview>("readEmail", token, fileName, streamRef);
    }

    public async Task<string?> DetectExtension(Stream content, CancellationToken token)
    {
        using var streamRef = new DotNetStreamReference(content);

        return await this.Invoke<string?>("detectExtension", token, streamRef);
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
}

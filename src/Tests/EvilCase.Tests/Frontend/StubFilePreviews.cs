using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// No library runs in a render test; this internal interface cannot be substituted with NSubstitute
/// without a DynamicProxyGenAssembly2 visibility grant, so a plain recorder stands in instead.
/// </summary>
internal sealed class StubFilePreviews : IFilePreviews
{
    public List<string> Converted { get; } = [];

    public List<string> RenderedDocuments { get; } = [];

    public List<string> RenderedPdfs { get; } = [];

    public List<byte[]> RenderedPdfContents { get; } = [];

    public int PdfPages { get; set; } = 3;

    public int DocumentPages { get; set; } = 4;

    public int DisposedPdfs { get; private set; }

    /// <summary>
    /// The view the last PDF reports its page, zoom and matches to, as the library would.
    /// </summary>
    public DotNetObjectReference<PreviewToolbar>? PdfView { get; private set; }

    public List<PreviewZoom> Zooms { get; } = [];

    public List<int> PdfPagesGoneTo { get; } = [];

    public List<(string Query, PreviewFind Find)> Finds { get; } = [];

    /// <summary>
    /// The view the last text or document reports its zoom and matches to, as the library would.
    /// </summary>
    public DotNetObjectReference<PreviewToolbar>? TextView { get; private set; }

    public int OpenedTextViews { get; private set; }

    public int DisposedViews { get; private set; }

    public List<PreviewZoom> ViewZooms { get; } = [];

    public List<int> ViewPagesGoneTo { get; } = [];

    public List<(string Query, PreviewFind Find)> ViewFinds { get; } = [];

    public List<(string Text, CodeLanguage Language, bool Truncated)> RenderedCode { get; } = [];

    /// <summary>
    /// What the library makes of the code it is given; a cut text never formats.
    /// </summary>
    public CodeView Code { get; set; } = new(Formatted: true, Invalid: false, LinesCut: false);

    public List<bool> ShownCode { get; } = [];

    public int DisposedCode { get; private set; }

    public List<int> RequestedRows { get; } = [];

    public IReadOnlyList<SheetPreview> Workbook { get; set; } = [];

    public EmailPreview Email { get; set; } = new(From: "", To: [], Cc: [], Subject: "", Date: null, Body: "", Truncated: false, Attachments: []);

    /// <summary>
    /// The extension the library finds in any content.
    /// </summary>
    public string? DetectedExtension { get; set; }

    public List<byte[]> Detected { get; } = [];

    /// <summary>
    /// What a library does with a file it cannot parse.
    /// </summary>
    public bool Fails { get; set; }

    public Task<string> CreatePngUrl(Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        var url = $"blob:converted/{this.Converted.Count}";
        this.Converted.Add(url);

        return Task.FromResult(url);
    }

    public async Task<int> RenderDocument(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        this.ThrowWhenFailing();

        using var reader = new StreamReader(content);
        this.RenderedDocuments.Add(await reader.ReadToEndAsync(token));
        this.TextView = view;

        return this.DocumentPages;
    }

    public Task OpenTextView(ElementReference stage, DotNetObjectReference<PreviewToolbar> view)
    {
        this.OpenedTextViews++;
        this.TextView = view;

        return Task.CompletedTask;
    }

    public Task<CodeView> RenderCode(ElementReference content, string text, CodeLanguage language, bool truncated, CancellationToken token)
    {
        this.ThrowWhenFailing();
        this.RenderedCode.Add((text, language, truncated));

        return Task.FromResult(truncated ? this.Code with { Formatted = false, Invalid = false } : this.Code);
    }

    public Task<bool> ShowCode(ElementReference content, bool formatted)
    {
        this.ShownCode.Add(formatted);

        return Task.FromResult(this.Code.LinesCut);
    }

    public Task DisposeCode(ElementReference content)
    {
        this.DisposedCode++;

        return Task.CompletedTask;
    }

    public Task ZoomView(ElementReference element, PreviewZoom zoom)
    {
        this.ViewZooms.Add(zoom);

        return Task.CompletedTask;
    }

    public Task GoToViewPage(ElementReference element, int page)
    {
        this.ViewPagesGoneTo.Add(page);

        return Task.CompletedTask;
    }

    public Task FindInView(ElementReference element, string query, PreviewFind find)
    {
        this.ViewFinds.Add((query, find));

        return Task.CompletedTask;
    }

    public Task DisposeView(ElementReference element)
    {
        this.DisposedViews++;

        return Task.CompletedTask;
    }

    public Task<int> RenderPdf(ElementReference host, string url, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        this.ThrowWhenFailing();
        this.RenderedPdfs.Add(url);
        this.PdfView = view;

        return Task.FromResult(this.PdfPages);
    }

    public async Task<int> RenderPdf(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        this.ThrowWhenFailing();

        await using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);
        this.RenderedPdfContents.Add(bytes.ToArray());
        this.PdfView = view;

        return this.PdfPages;
    }

    public Task ZoomPdf(ElementReference host, PreviewZoom zoom)
    {
        this.Zooms.Add(zoom);

        return Task.CompletedTask;
    }

    public Task GoToPdfPage(ElementReference host, int page)
    {
        this.PdfPagesGoneTo.Add(page);

        return Task.CompletedTask;
    }

    public Task FindInPdf(ElementReference host, string query, PreviewFind find)
    {
        this.Finds.Add((query, find));

        return Task.CompletedTask;
    }

    public Task DisposePdf(ElementReference host)
    {
        this.DisposedPdfs++;

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<SheetPreview>> ReadWorkbook(Stream content, int maxRows, CancellationToken token)
    {
        this.ThrowWhenFailing();
        this.RequestedRows.Add(maxRows);

        return Task.FromResult(this.Workbook);
    }

    public Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        return Task.FromResult(this.Email);
    }

    public async Task<string?> DetectExtension(Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        await using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);
        this.Detected.Add(bytes.ToArray());

        return this.DetectedExtension;
    }

    private void ThrowWhenFailing()
    {
        if (this.Fails)
            throw new JSException("The file is not a valid one.");
    }
}

using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

/// <summary>
/// No library runs in a render test; this internal interface cannot be substituted with NSubstitute
/// without a DynamicProxyGenAssembly2 visibility grant, so a plain recorder stands in instead.
/// </summary>
internal sealed class StubFilePreviews : IFilePreviews
{
    private int converted;

    public List<string> RenderedPdfs { get; } = [];

    public List<byte[]> RenderedPdfContents { get; } = [];

    public int DisposedPdfs { get; private set; }

    /// <summary>
    /// The view the last PDF reports its page, zoom and matches to, as the library would.
    /// </summary>
    public DotNetObjectReference<PreviewToolbar>? PdfView { get; private set; }

    /// <summary>
    /// The view the last text or document reports its zoom and matches to, as the library would.
    /// </summary>
    public DotNetObjectReference<PreviewToolbar>? TextView { get; private set; }

    public int OpenedTextViews { get; private set; }

    /// <summary>
    /// When set, a text view opens once it completes, as a script still loading would.
    /// </summary>
    public TaskCompletionSource? TextViewLoading { get; set; }

    public int DisposedViews { get; private set; }

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

        return Task.FromResult(string.Create(CultureInfo.InvariantCulture, $"blob:converted/{this.converted++}"));
    }

    public Task<int> RenderDocument(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        this.ThrowWhenFailing();
        this.TextView = view;

        return Task.FromResult(4);
    }

    public async Task OpenTextView(ElementReference stage, DotNetObjectReference<PreviewToolbar> view)
    {
        if (this.TextViewLoading is { } loading)
            await loading.Task;

        // Hands the view to the script as the interop would, which a disposed view refuses.
        _ = view.Value;
        this.OpenedTextViews++;
        this.TextView = view;
    }

    public Task ZoomView(ElementReference element, PreviewZoom zoom)
    {
        return Task.CompletedTask;
    }

    public Task GoToViewPage(ElementReference element, int page)
    {
        return Task.CompletedTask;
    }

    public Task FindInView(ElementReference element, string query, PreviewFind find)
    {
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

        return Task.FromResult(3);
    }

    public async Task<int> RenderPdf(ElementReference host, Stream content, DotNetObjectReference<PreviewToolbar> view, CancellationToken token)
    {
        this.ThrowWhenFailing();

        await using var bytes = new MemoryStream();

        await content.CopyToAsync(bytes, token);
        this.RenderedPdfContents.Add(bytes.ToArray());
        this.PdfView = view;

        return 3;
    }

    public Task ZoomPdf(ElementReference host, PreviewZoom zoom)
    {
        return Task.CompletedTask;
    }

    public Task GoToPdfPage(ElementReference host, int page)
    {
        return Task.CompletedTask;
    }

    public Task FindInPdf(ElementReference host, string query, PreviewFind find)
    {
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

        return Task.FromResult<IReadOnlyList<SheetPreview>>([]);
    }

    public Task<EmailPreview> ReadEmail(string fileName, Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        return Task.FromResult(new EmailPreview(From: "", To: [], Cc: [], Subject: "", Date: null, Body: "", Truncated: false, Attachments: []));
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

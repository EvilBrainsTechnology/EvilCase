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
    public List<string> Converted { get; } = [];

    public List<string> RenderedDocuments { get; } = [];

    public List<string> RenderedPdfs { get; } = [];

    public int PdfPages { get; set; } = 3;

    public int DisposedPdfs { get; private set; }

    /// <summary>
    /// The view the last PDF reports its page, zoom and matches to, as the library would.
    /// </summary>
    public DotNetObjectReference<PdfPreview>? PdfView { get; private set; }

    public List<PdfZoom> Zooms { get; } = [];

    public List<int> PdfPagesGoneTo { get; } = [];

    public List<(string Query, PdfFind Find)> Finds { get; } = [];

    public List<int> RequestedRows { get; } = [];

    public IReadOnlyList<SheetPreview> Workbook { get; set; } = [];

    public EmailPreview Email { get; set; } = new(From: "", To: [], Cc: [], Subject: "", Date: null, Body: "", Truncated: false, Attachments: []);

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

    public async Task RenderDocument(ElementReference host, Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        using var reader = new StreamReader(content);
        this.RenderedDocuments.Add(await reader.ReadToEndAsync(token));
    }

    public Task<int> RenderPdf(ElementReference host, string url, DotNetObjectReference<PdfPreview> view, CancellationToken token)
    {
        this.ThrowWhenFailing();
        this.RenderedPdfs.Add(url);
        this.PdfView = view;

        return Task.FromResult(this.PdfPages);
    }

    public Task ZoomPdf(ElementReference host, PdfZoom zoom)
    {
        this.Zooms.Add(zoom);

        return Task.CompletedTask;
    }

    public Task GoToPdfPage(ElementReference host, int page)
    {
        this.PdfPagesGoneTo.Add(page);

        return Task.CompletedTask;
    }

    public Task FindInPdf(ElementReference host, string query, PdfFind find)
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

    private void ThrowWhenFailing()
    {
        if (this.Fails)
            throw new JSException("The file is not a valid one.");
    }
}

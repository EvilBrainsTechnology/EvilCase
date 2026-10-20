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
    public List<(string FileName, string Url)> Converted { get; } = [];

    public List<string> RenderedDocuments { get; } = [];

    public List<string> RenderedPdfs { get; } = [];

    public int PdfPages { get; set; } = 3;

    public int DisposedPdfs { get; private set; }

    public List<int> RequestedRows { get; } = [];

    public IReadOnlyList<SheetPreview> Workbook { get; set; } = [];

    public EmailPreview Email { get; set; } = new(From: "", To: [], Cc: [], Subject: "", Date: null, Body: "", Truncated: false, Attachments: []);

    /// <summary>
    /// What a library does with a file it cannot parse.
    /// </summary>
    public bool Fails { get; set; }

    public Task<string> CreatePngUrl(string fileName, Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        var url = $"blob:converted/{this.Converted.Count}";
        this.Converted.Add((fileName, url));

        return Task.FromResult(url);
    }

    public async Task RenderDocument(ElementReference host, Stream content, CancellationToken token)
    {
        this.ThrowWhenFailing();

        using var reader = new StreamReader(content);
        this.RenderedDocuments.Add(await reader.ReadToEndAsync(token));
    }

    public Task<int> RenderPdf(ElementReference host, string url, CancellationToken token)
    {
        this.ThrowWhenFailing();
        this.RenderedPdfs.Add(url);

        return Task.FromResult(this.PdfPages);
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

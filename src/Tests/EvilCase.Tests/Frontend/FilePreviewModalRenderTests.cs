using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalRenderTests
{
    [Test]
    public void SteppingToAnotherFileReleasesThePdf()
    {
        using var ctx = NewContext(out _, out var previews);

        var pdf = File("a.pdf");
        var text = File("b.txt");
        var component = Render(ctx, [pdf, text], pdf.FileId, content: "řádek");

        component.WaitForAssertion(() => Assert.That(previews.RenderedPdfs, Has.Count.EqualTo(1)));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, text.FileId));

        Assert.That(previews.DisposedPdfs, Is.EqualTo(1));
    }

    [Test]
    public void SteppingFromOnePdfToAnotherReleasesTheFirstAndOpensTheSecond()
    {
        using var ctx = NewContext(out _, out var previews);

        var first = File("a.pdf");
        var second = File("b.pdf");
        var component = Render(ctx, [first, second], first.FileId);

        component.WaitForAssertion(() => Assert.That(previews.RenderedPdfs, Has.Count.EqualTo(1)));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, second.FileId));

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(previews.DisposedPdfs, Is.EqualTo(1));
                Assert.That(previews.RenderedPdfs, Is.EqualTo([$"/api/files/{first.FileId}/content", $"/api/files/{second.FileId}/content"]));
                Assert.That(component.FindAll(".ec-preview-pdf"), Has.Count.EqualTo(1));
            }
        });
    }

    [TestCase("a.pdf", 1)]
    [TestCase("a.mp4", 1)]
    [TestCase("a.svg", 0)]
    public void APreviewTheBrowserRequestsItselfRenewsAnExpiringSessionFirst(string fileName, int checks)
    {
        using var ctx = NewContext(out _);

        var file = File(fileName);
        Render(ctx, [file], file.FileId);

        Assert.That(ctx.Services.GetRequiredService<StubAuthSession>().FreshnessChecks, Is.EqualTo(checks), "a plain browser request carries no renewal of its own");
    }

    [TestCase("a.pdf")]
    [TestCase("a.mp3")]
    [TestCase("a.mp4")]
    public void APreviewTheBrowserStreamsKeepsTheSessionFreshUntilAnotherFileTakesOver(string fileName)
    {
        using var ctx = NewContext(out _);

        var streamed = File(fileName);
        var text = File("b.txt");
        var component = Render(ctx, [streamed, text], streamed.FileId);
        var session = ctx.Services.GetRequiredService<StubAuthSession>();

        Assert.That(session.KeptFresh, Has.Count.EqualTo(1));
        Assert.That(session.KeptFresh[0].IsCancellationRequested, Is.False);

        component.Render(parameters => parameters.Add(static modal => modal.FileId, text.FileId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.KeptFresh, Has.Count.EqualTo(1), "a file read as bytes needs no renewal while shown");
            Assert.That(session.KeptFresh[0].IsCancellationRequested, Is.True);
        }
    }

    [Test]
    public async Task ClosingAStreamedPreviewStopsKeepingTheSessionFresh()
    {
        await using var ctx = NewContext(out _);

        var file = File("a.mp4");
        var component = Render(ctx, [file], file.FileId);
        var session = ctx.Services.GetRequiredService<StubAuthSession>();

        await component.Instance.DisposeAsync();

        Assert.That(session.KeptFresh.Single().IsCancellationRequested, Is.True);
    }

    [TestCase("img.ec-preview-image", "a.png")]
    [TestCase("audio.ec-preview-audio", "a.mp3")]
    [TestCase("video.ec-preview-video", "a.mp4")]
    public async Task AnElementThatCannotReadTheFileFallsBackToTheMessageWithTheDownload(string selector, string fileName)
    {
        await using var ctx = NewContext(out _);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId);

        await component.Find(selector).TriggerEventAsync("onerror", new Microsoft.AspNetCore.Components.Web.ErrorEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"));
            Assert.That(component.FindAll(selector), Is.Empty);
        }
    }

    [Test]
    public void ACodeFileThatFailsToFormatForAnotherReasonShowsTheOriginalWithTheSwitchDisabled()
    {
        using var ctx = NewContext(out _, out _, out var editors);

        editors.Code = new CodeView(Formatted: false, Invalid: false);

        var file = File("data.json");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-toolbar-note").TextContent.Trim(), Is.EqualTo("Soubor se nepodařilo naformátovat, ukazuje se originál."));
            Assert.That(ToolbarButton(component, "Formátovat").HasAttribute("disabled"), Is.True);
        }
    }

    [TestCase("a.docx")]
    public void SteppingToAnotherFileReleasesTheZoomAndSearch(string fileName)
    {
        using var ctx = NewContext(out _, out var previews);

        var first = File(fileName);
        var second = File("b.png");
        var component = Render(ctx, [first, second], first.FileId);

        component.WaitForAssertion(() => Assert.That(previews.TextView, Is.Not.Null));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, second.FileId));

        Assert.That(previews.DisposedViews, Is.EqualTo(1));
    }

    [Test]
    public void SteppingFromAFailedFileToAnotherShowsTheOtherAsUsual()
    {
        using var ctx = NewContext(out _, out var previews, out var editors);

        previews.Fails = true;

        var broken = File("Rozbite.docx");
        var text = File("Poznámky.txt");
        var component = Render(ctx, [broken, text], broken.FileId, content: "řádek");

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-message-title"), Has.Count.EqualTo(1)));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, text.FileId));

        Assert.That(editors.Opened.Single().Text, Is.EqualTo("řádek"));
    }

    [Test]
    public void NextAndPreviousWrapAroundTheCardsFiles()
    {
        using var ctx = NewContext(out _);

        var files = new FileListItem[] { File("a.pdf"), File("b.png"), File("c.txt") };
        var shown = new List<Guid>();
        var component = Render(ctx, files, files[2].FileId, onFileIdChanged: shown.Add);

        component.Find("button[aria-label='Další soubor']").Click();
        component.Find("button[aria-label='Předchozí soubor']").Click();

        Assert.That(shown, Is.EqualTo([files[0].FileId, files[1].FileId]), "next from the last file shows the first, previous from the last shows the one before");
    }

    [Test]
    public void SwitchingFilesRevokesTheBlobOfTheOneLeft()
    {
        using var ctx = NewContext(out var blobs);

        var files = new FileListItem[] { File("a.svg"), File("b.svg") };
        var component = Render(ctx, files, files[0].FileId);

        component.Render(parameters => parameters.Add(static modal => modal.FileId, files[1].FileId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blobs.Revoked, Is.EqualTo(["blob:test/0"]), "the left file's URL is revoked");
            Assert.That(component.Find("img.ec-preview-image").GetAttribute("src"), Is.EqualTo("blob:test/1"));
        }
    }

    [Test]
    public void LeavingAFileShownByItsContentAddressRevokesNothing()
    {
        using var ctx = NewContext(out var blobs);

        var files = new FileListItem[] { File("a.pdf"), File("b.png") };
        var component = Render(ctx, files, files[0].FileId);

        component.Render(parameters => parameters.Add(static modal => modal.FileId, files[1].FileId));

        Assert.That(blobs.Revoked, Is.Empty, "a content address is no blob URL");
    }

    [Test]
    public async Task ClosingTheModalRevokesTheBlob()
    {
        await using var ctx = NewContext(out var blobs);

        var file = File("a.svg");
        var component = Render(ctx, [file], file.FileId);

        await component.Instance.DisposeAsync();

        Assert.That(blobs.Revoked, Is.EqualTo(["blob:test/0"]));
    }

    [Test]
    public void AnOlderTextFileIsReadFromItsVersion()
    {
        using var ctx = NewContext(out _);

        var requested = new List<int?>();
        var file = Versioned("a.txt", 3, 3);

        Render(
            ctx,
            [file],
            file.FileId,
            loadVersion: (_, number, _) =>
            {
                requested.Add(number);

                return Task.FromResult<Stream>(new MemoryStream("starší"u8.ToArray()));
            },
            version: 2);

        Assert.That(requested, Is.EqualTo(new int?[] { 2 }));
    }

    [Test]
    public async Task TheDownloadOfAnOlderVersionDownloadsThatVersion()
    {
        await using var ctx = NewContext(out _);

        var downloaded = new List<string>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName), version: 2);

        await component.Find("button[aria-label='Stáhnout']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctx.Services.GetRequiredService<StubFileDownloader>().Downloaded.Single(), Is.EqualTo((file.FileId, (int?)2, "a.txt")));
            Assert.That(downloaded, Is.Empty);
        }
    }

    [Test]
    public async Task PickingAVersionFromTheListRaisesItsNumberAndTheCurrentRaisesNull()
    {
        await using var ctx = NewContext(out _);

        var changed = new List<int?>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, onVersionChanged: changed.Add, loadVersions: static (_, _) => Task.FromResult(VersionItems()));

        await component.Find("button[aria-label='Verze, 3']").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-versions .ec-popover-trigger").ClickAsync(new MouseEventArgs());

        var options = component.FindAll(".ec-preview-versions-option");

        Assert.That(options.Select(static option => Normalize(option.QuerySelector(".ec-preview-versions-option-title")!.TextContent)), Is.EqualTo(["Verze 3 aktuální", "Verze 2", "Verze 1"]));

        await options[1].ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-versions .ec-popover-trigger").ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-preview-versions-option")[0].ClickAsync(new MouseEventArgs());

        Assert.That(changed, Is.EqualTo(new int?[] { 2, null }));
    }

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static FileListItem Versioned(string fileName, int version, int count)
    {
        return File(fileName) with { Version = version, VersionCount = count };
    }

    private static IReadOnlyList<FileVersionItem> VersionItems()
    {
        return
        [
            new FileVersionItem { Number = 3, SizeBytes = 3072, Created = new DateTime(2025, 9, 4, 10, 0, 0, DateTimeKind.Utc), UploaderEmail = "jan.priklad@example.cz" },
            new FileVersionItem { Number = 2, SizeBytes = 2048, Created = new DateTime(2025, 9, 3, 10, 0, 0, DateTimeKind.Utc), UploaderEmail = "jana.ukazkova@example.cz" },
            new FileVersionItem { Number = 1, SizeBytes = 1024, Created = new DateTime(2025, 9, 2, 10, 0, 0, DateTimeKind.Utc), UploaderEmail = "jan.priklad@example.cz" },
        ];
    }

    private static AngleSharp.Dom.IElement ToolbarButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-preview-toolbar button[aria-label='{label}']");
    }

    private static BunitContext NewContext(out StubBlobUrls blobs)
    {
        return NewContext(out blobs, out _);
    }

    private static BunitContext NewContext(out StubBlobUrls blobs, out StubFilePreviews previews)
    {
        return NewContext(out blobs, out previews, out _);
    }

    private static BunitContext NewContext(out StubBlobUrls blobs, out StubFilePreviews previews, out StubTextEditors editors)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBrowserHistory>(new StubBrowserHistory());

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        editors = new StubTextEditors();
        ctx.Services.AddSingleton<ITextEditors>(editors);
        var downloader = new StubFileDownloader();
        ctx.Services.AddSingleton(downloader);
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());

        return ctx;
    }

    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        string content = "abc",
        Func<FileListItem, CancellationToken, Task<Stream>>? load = null,
        Func<FileListItem, string, CancellationToken, Task>? rename = null,
        Action<FileListItem>? onDownload = null,
        Action? onRenamed = null,
        Action<Guid>? onFileIdChanged = null,
        Func<FileListItem, int?, CancellationToken, Task<Stream>>? loadVersion = null,
        int? version = null,
        Action<int?>? onVersionChanged = null,
        Func<FileListItem, CancellationToken, Task<IReadOnlyList<FileVersionItem>>>? loadVersions = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<FileListItem>? onManageVersions = null)
    {
        var loadContent = load ?? ((_, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content))));
        var loadNumbered = loadVersion ?? (async (item, _, token) => await loadContent(item, token));
        var renameFile = rename ?? (static (_, _, _) => Task.CompletedTask);
        var listVersions = loadVersions ?? (static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]));
        var upload = uploadVersion ?? (static (_, _) => Task.FromResult<(string?, string?)>((null, null)));

        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, loadNumbered)
            .Add(static modal => modal.Version, version)
            .Add(static modal => modal.VersionChanged, changed => onVersionChanged?.Invoke(changed))
            .Add(static modal => modal.LoadVersions, listVersions)
            .Add(static modal => modal.UploadVersion, upload)
            .Add(static modal => modal.OnManageVersions, item => onManageVersions?.Invoke(item))
            .Add(static modal => modal.Rename, renameFile)
            .Add(static modal => modal.OnDownload, item => onDownload?.Invoke(item))
            .Add(static modal => modal.OnRenamed, () => onRenamed?.Invoke())
            .Add(static modal => modal.FileIdChanged, id => onFileIdChanged?.Invoke(id))
            .Add(static modal => modal.Open, value: true));
    }

    private static FileListItem File(string fileName)
    {
        return new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = fileName,
            SizeBytes = 1024,
            Created = new DateTime(2025, 9, 2, 10, 0, 0, DateTimeKind.Utc),
            Version = 1,
            VersionCount = 1,
        };
    }
}

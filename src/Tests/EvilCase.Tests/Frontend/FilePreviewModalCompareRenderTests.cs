using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalCompareRenderTests
{
    private const string Header = ".ec-modal-large .ec-modal-header";

    [Test]
    public async Task CompareOpensWithThePreviousVersionWithoutTheEditingActions()
    {
        await using var ctx = NewContext(out var compares, out _);

        var file = File("a.txt", 3);
        var component = Render(ctx, file, Texts("a\n", "b\n", "c\n"));

        await component.Find("button[aria-label='Porovnat']").ClickAsync(new MouseEventArgs());

        var header = component.Find(Header).TextContent;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(header, Does.Contain("Porovnání verze 2 s verzí 3"));
            Assert.That(compares.Opened.Select(static opened => (opened.Older, opened.Newer)), Is.EqualTo([("b\n", "c\n")]));
            Assert.That(header, Does.Not.Contain("Upravit").And.Not.Contain("Nová verze").And.Not.Contain("Přejmenovat"), "the compare only reads");
            Assert.That(component.FindAll($"{Header} button[aria-label='Verze, 3']"), Is.Empty, "the version shown is chosen in the compare");
        }

        await component.Find("button[aria-label='Zavřít porovnání']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("button[aria-label='Upravit']"), Has.Count.EqualTo(1), "closing returns to the preview");
            Assert.That(compares.Closed, Is.EqualTo(1));
        }
    }

    [Test]
    public void TheOldestVersionComparesWithTheNextOne()
    {
        using var ctx = NewContext(out var compares, out _);

        var file = File("a.json", 3);
        var component = Render(ctx, file, Texts("{}", "[]", "1"), version: 1, compareOnOpen: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(Header).TextContent, Does.Contain("Porovnání verze 1 s verzí 2"));
            Assert.That(compares.Opened, Is.EqualTo([("{}", "[]", (CodeLanguage?)CodeLanguage.Json, true)]), "code compares formatted");
        }
    }

    [Test]
    public void CodeEqualFormattedComparesTheOriginalAndKeepsTheToggle()
    {
        using var ctx = NewContext(out var compares, out _);

        compares.Shown = new CompareView(Formatted: false, Formattable: true, Added: 1, Removed: 1, Changes: 1);

        var file = File("a.json", 2);
        var component = Render(ctx, file, Texts("{\"a\":1}", "{ \"a\": 1 }"), compareOnOpen: true);
        var toggle = component.FindAll("[aria-label='Podoba textu'] button");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toggle.Select(static button => (button.GetAttribute("aria-pressed"), button.HasAttribute("disabled"))), Is.EqualTo([("true", false), ("false", false)]), "versions equal formatted show the original and still format");
            Assert.That(component.FindAll(".ec-toolbar-notes"), Is.Empty, "both versions format");
        }
    }

    [Test]
    public async Task VersionsDifferingOnlyInLineBreaksAreEqual()
    {
        await using var ctx = NewContext(out var compares, out _);

        var file = File("a.txt", 2);
        var component = Render(ctx, file, Texts("a\nb\n", "a\r\nb\r\n"), compareOnOpen: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-text-compare-notice-same").TextContent, Does.Contain("Verze jsou shodné"));
            Assert.That(compares.Opened, Is.Empty, "equal versions open no compare");
        }
    }

    [Test]
    public void ASideOverTheLimitSaysWhyAndIsNeverRead()
    {
        using var ctx = NewContext(out var compares, out _);

        var read = new List<int?>();
        var file = File("a.txt", 2);
        var component = Render(ctx, file, Texts("a\n", "b\n"), compareOnOpen: true, sizes: [FileTypeDisplay.MaxTextBytes + 1, 10], onRead: read.Add);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-text-compare-notice-warning").TextContent, Does.Contain("Verzi 1 nelze porovnat: soubor je větší než 1 MB"));
            Assert.That(read, Does.Not.Contain(1), "the compare never reads the side over the limit");
            Assert.That(compares.Opened, Is.Empty);
        }
    }

    [Test]
    public void ASideNotInUtf8SaysWhy()
    {
        using var ctx = NewContext(out var compares, out _);

        var file = File("a.txt", 2);
        var component = Render(ctx, file, [[0x9E, 0x0A], "b\n"u8.ToArray()], compareOnOpen: true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-text-compare-notice-warning").TextContent, Does.Contain("Verzi 1 nelze porovnat: soubor není v kódování UTF-8"));
            Assert.That(compares.Opened, Is.Empty);
        }
    }

    [Test]
    public async Task AFailedReadShowsTheErrorAndReadsAgainOnRetry()
    {
        await using var ctx = NewContext(out var compares, out _);

        var fails = true;
        var file = File("a.txt", 2);
        var content = Texts("a\n", "b\n");
        var component = Render(ctx, file, content, compareOnOpen: true, onRead: Read);

        Assert.That(component.Find(".ec-text-compare-notice-danger").TextContent, Does.Contain("Verzi 1 se nepodařilo načíst."));

        fails = false;
        await component
            .FindAll(".ec-text-compare-state button")
            .Single(static button => button.TextContent.Contains("Zkusit znovu", StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        Assert.That(compares.Opened.Select(static opened => (opened.Older, opened.Newer)), Is.EqualTo([("a\n", "b\n")]));

        void Read(int? version)
        {
            if (version == 1 && fails)
                throw new HttpRequestException("The server is away.");
        }
    }

    [Test]
    public async Task EachPickerRulesOutTheVersionsThatDoNotStandOnItsSide()
    {
        await using var ctx = NewContext(out _, out _);

        var file = File("a.txt", 3);
        var component = Render(ctx, file, Texts("a\n", "b\n", "c\n"), compareOnOpen: true);

        Assert.That(await Options(component, "Starší"), Is.EqualTo([("Verze 3", false), ("Verze 2", true), ("Verze 1", true)]), "the older side takes a version before the newer");

        await component.Find(".ec-popover-backdrop").ClickAsync(new MouseEventArgs());

        Assert.That(await Options(component, "Novější"), Is.EqualTo([("Verze 3", true), ("Verze 2", false), ("Verze 1", false)]), "the newer side takes a version after the older");
    }

    [Test]
    public async Task StahnoutDownloadsTheNewerVersion()
    {
        await using var ctx = NewContext(out _, out var downloader);

        var downloads = new List<FileListItem>();
        var file = File("a.txt", 3);
        var component = Render(ctx, file, Texts("a\n", "b\n", "c\n"), compareOnOpen: true, onDownload: downloads.Add);

        await component.Find("button[aria-label='Stáhnout verzi 3']").ClickAsync(new MouseEventArgs());

        await Pick(component, "Starší", "Verze 1");
        await Pick(component, "Novější", "Verze 2");
        await component.Find("button[aria-label='Stáhnout verzi 2']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloads, Is.EqualTo([file]), "the current version downloads as the preview's does");
            Assert.That(downloader.Downloaded, Is.EqualTo([(file.FileId, (int?)2, "a.txt")]));
            Assert.That(component.Find(Header).TextContent, Does.Contain("Porovnání verze 1 s verzí 2"));
        }
    }

    [Test]
    public async Task APickedVersionNoLongerListedFallsBackToThePairTheCompareOpenedWith()
    {
        await using var ctx = NewContext(out var compares, out _);

        var file = File("a.txt", 4);
        var component = Render(ctx, file, Texts("a\n", "b\n", "c\n", "d\n"), compareOnOpen: true);

        await Pick(component, "Starší", "Verze 1");

        IReadOnlyList<FileVersionItem> left = [Version(4, 2), Version(3, 2), Version(2, 2)];
        component.Render(parameters => parameters
            .Add(static modal => modal.Files, [file with { VersionCount = 3 }])
            .Add(static modal => modal.LoadVersions, (_, _) => Task.FromResult(left)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(Header).TextContent, Does.Contain("Porovnání verze 3 s verzí 4"));
            Assert.That(compares.Opened.Select(static opened => (opened.Older, opened.Newer)).Last(), Is.EqualTo(("c\n", "d\n")), "a deleted version gives way to the pair the compare opened with");
        }
    }

    [Test]
    public void AFileLeftWithOneVersionEndsTheCompare()
    {
        using var ctx = NewContext(out _, out _);

        var file = File("a.txt", 2);
        var component = Render(ctx, file, Texts("a\n", "b\n"), compareOnOpen: true);

        component.Render(parameters => parameters.Add(static modal => modal.Files, [file with { VersionCount = 1 }]));

        Assert.That(component.FindAll("button[aria-label='Upravit']"), Has.Count.EqualTo(1), "one version has nothing to compare");
    }

    private static async Task<List<(string Title, bool Enabled)>> Options(IRenderedComponent<FilePreviewModal> component, string side)
    {
        await Picker(component, side).ClickAsync(new MouseEventArgs());

        return
        [
            .. component
                .FindAll(".ec-popover .ec-preview-versions-option")
                .Select(static option => (option.QuerySelector(".ec-preview-versions-option-title")!.TextContent.Replace("aktuální", "", StringComparison.Ordinal).Trim(), !option.HasAttribute("disabled"))),
        ];
    }

    private static async Task Pick(IRenderedComponent<FilePreviewModal> component, string side, string title)
    {
        await Picker(component, side).ClickAsync(new MouseEventArgs());
        await component
            .FindAll(".ec-popover .ec-preview-versions-option")
            .Single(option => option.QuerySelector(".ec-preview-versions-option-title")!.TextContent.Trim().StartsWith(title, StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());
    }

    private static AngleSharp.Dom.IElement Picker(IRenderedComponent<FilePreviewModal> component, string side)
    {
        return component
            .FindAll(".ec-text-compare-version")
            .Single(version => string.Equals(version.QuerySelector(".ec-text-compare-version-label")!.TextContent, side, StringComparison.Ordinal))
            .QuerySelector(".ec-popover-trigger")!;
    }

    private static byte[][] Texts(params string[] texts)
    {
        return [.. texts.Select(static text => Encoding.UTF8.GetBytes(text))];
    }

    private static BunitContext NewContext(out StubTextCompares compares, out StubFileDownloader downloader)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        compares = new StubTextCompares();
        downloader = new StubFileDownloader();
        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());
        ctx.Services.AddSingleton<ITextEditors>(new StubTextEditors());
        ctx.Services.AddSingleton<ITextCompares>(compares);
        ctx.Services.AddSingleton<IMarkdownEditors>(new StubMarkdownEditors());
        ctx.Services.AddSingleton(new StubBrowserHistory());
        ctx.Services.AddSingleton<IBrowserHistory>(static services => services.GetRequiredService<StubBrowserHistory>());
        ctx.Services.AddSingleton(Substitute.For<IFileSignaturesClient>());

        var signers = Substitute.For<IFileSignersClient>();
        signers.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [] });
        signers.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignersResponse>().Task);
        ctx.Services.AddSingleton(signers);

        return ctx;
    }

    // contents holds the text of each version, the oldest first; sizes their sizes, by default their lengths.
    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        FileListItem file,
        byte[][] contents,
        int? version = null,
        bool compareOnOpen = false,
        long[]? sizes = null,
        Action<int?>? onRead = null,
        Action<FileListItem>? onDownload = null)
    {
        IReadOnlyList<FileVersionItem> versions =
        [
            .. Enumerable.Range(1, contents.Length).Reverse().Select(number => Version(number, sizes?[number - 1] ?? contents[number - 1].Length)),
        ];

        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.CompareOnOpen, compareOnOpen)
            .Add(static modal => modal.Files, [file])
            .Add(static modal => modal.FileId, file.FileId)
            .Add(static modal => modal.LoadContent, Load)
            .Add(static modal => modal.Version, version)
            .Add(static modal => modal.LoadVersions, (_, _) => Task.FromResult(versions))
            .Add(static modal => modal.UploadVersion, static (_, _) => Task.FromResult<(string?, string?)>((null, null)))
            .Add(static modal => modal.Rename, static (_, _, _) => Task.CompletedTask)
            .Add(static modal => modal.OnDownload, item => onDownload?.Invoke(item))
            .Add(static modal => modal.Open, value: true));

        Task<Stream> Load(FileListItem item, int? number, CancellationToken _)
        {
            onRead?.Invoke(number);

            return Task.FromResult<Stream>(new MemoryStream(contents[(number ?? item.Version) - 1]));
        }
    }

    private static FileVersionItem Version(int number, long size)
    {
        return new FileVersionItem
        {
            Number = number,
            SizeBytes = size,
            Created = new DateTime(2025, 9, number, 10, 0, 0, DateTimeKind.Utc),
            UploaderEmail = "jana@example.test",
        };
    }

    private static FileListItem File(string fileName, int versions)
    {
        return new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = fileName,
            SizeBytes = 2,
            Created = new DateTime(2025, 9, versions, 10, 0, 0, DateTimeKind.Utc),
            Version = versions,
            VersionCount = versions,
        };
    }
}

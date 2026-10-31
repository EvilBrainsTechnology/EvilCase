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

public class FilePreviewModalEditorChangesRenderTests
{
    [Test]
    public async Task ZmenyCompareTheEditedTextWithTheVersionEditedUntilSwitchedOff()
    {
        await using var ctx = NewContext(out var editors, out _, out _);

        var component = await RenderEditing(ctx, File("a.txt", 3), Texts("a\n", "b\n", "c\n"));

        await ChangesButton(component).ClickAsync(new MouseEventArgs());
        await ChangesButton(component).ClickAsync(new MouseEventArgs());

        Assert.That(editors.Compared, Is.EqualTo(["c\n", null]));
    }

    [Test]
    public async Task PickingAnotherVersionComparesWithItsText()
    {
        await using var ctx = NewContext(out var editors, out _, out _);

        var read = new List<int?>();
        var component = await RenderEditing(ctx, File("a.txt", 3), Texts("a\n", "b\n", "c\n"), onRead: read.Add);

        await ChangesButton(component).ClickAsync(new MouseEventArgs());
        await Pick(component, "Verze 1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Compared[^1], Is.EqualTo("a\n"));
            Assert.That(read, Does.Contain(1));
        }
    }

    [Test]
    public async Task AVersionOverTheLimitSaysWhyAndIsNeverRead()
    {
        await using var ctx = NewContext(out var editors, out _, out _);

        var read = new List<int?>();
        var component = await RenderEditing(ctx, File("a.txt", 2), Texts("a\n", "b\n"), sizes: [FileTypeDisplay.MaxTextBytes + 1, 2], onRead: read.Add);

        await ChangesButton(component).ClickAsync(new MouseEventArgs());
        await Pick(component, "Verze 1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-warning").TextContent, Does.Contain("Verzi 1 nelze porovnat: soubor je větší než 1 MB"));
            Assert.That(read, Does.Not.Contain(1), "a version over the limit is never read");
            Assert.That(editors.Compared[^1], Is.Null);
        }
    }

    [Test]
    public async Task AVersionNotInUtf8SaysWhy()
    {
        await using var ctx = NewContext(out var editors, out _, out _);

        var component = await RenderEditing(ctx, File("a.txt", 2), [[0x9E, 0x0A], "b\n"u8.ToArray()]);

        await ChangesButton(component).ClickAsync(new MouseEventArgs());
        await Pick(component, "Verze 1");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-warning").TextContent, Does.Contain("Verzi 1 nelze porovnat: soubor není v kódování UTF-8"));
            Assert.That(editors.Compared[^1], Is.Null);
        }
    }

    [Test]
    public async Task AFailedReadShowsTheErrorAndReadsAgainOnRetry()
    {
        await using var ctx = NewContext(out var editors, out _, out _);

        var fails = true;
        var component = await RenderEditing(ctx, File("a.txt", 2), Texts("a\n", "b\n"), onRead: Read);

        await ChangesButton(component).ClickAsync(new MouseEventArgs());
        await Pick(component, "Verze 1");

        Assert.That(component.Find(".ec-alert").TextContent, Does.Contain("Verzi 1 se nepodařilo načíst."));

        fails = false;
        await component
            .FindAll(".ec-alert button")
            .Single(static button => button.TextContent.Contains("Zkusit znovu", StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        Assert.That(editors.Compared[^1], Is.EqualTo("a\n"));

        void Read(int? version)
        {
            if (version == 1 && fails)
                throw new HttpRequestException("The server is away.");
        }
    }

    [Test]
    public async Task FormattedMarkdownShowsItsChangesReadOnlyAndFollowsZpet()
    {
        await using var ctx = NewContext(out _, out var markdown, out var compares);

        var component = await RenderEditing(ctx, File("a.md", 2), Texts("# a\n", "# b\n"));

        markdown.Text = "# c\n";
        await component.InvokeAsync(async () => await component.FindComponent<MarkdownView>().Instance.NotifyChanged(dirty: true, [], []));
        await ChangesButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(compares.MarkdownOpened, Is.EqualTo([("# b\n", "# c\n")]));
            Assert.That(component.Find("button[aria-label='Tučně']").HasAttribute("disabled"), Is.True, "the formatted changes are read-only");
        }

        markdown.Text = "# b\n";
        await component.Find("button[aria-label='Zpět']").ClickAsync(new MouseEventArgs());

        Assert.That(compares.MarkdownOpened[^1], Is.EqualTo(("# b\n", "# b\n")), "Zpět changes the text compared");
    }

    [Test]
    public async Task FormattedMarkdownComparesTheTextTypedWhileTheVersionCouldNotShow()
    {
        await using var ctx = NewContext(out _, out var markdown, out var compares);

        var fails = true;
        var component = await RenderEditing(ctx, File("a.md", 3), Texts("# a\n", "# b\n", "# c\n"), onRead: Read);

        await ChangesButton(component).ClickAsync(new MouseEventArgs());
        await Pick(component, "Verze 1");

        markdown.Text = "# d\n";
        await component.InvokeAsync(async () => await component.FindComponent<MarkdownView>().Instance.NotifyChanged(dirty: true, [], []));

        fails = false;
        await component
            .FindAll(".ec-alert button")
            .Single(static button => button.TextContent.Contains("Zkusit znovu", StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        Assert.That(compares.MarkdownOpened[^1], Is.EqualTo(("# a\n", "# d\n")), "the compare shows the text typed while the version could not show");

        void Read(int? version)
        {
            if (version == 1 && fails)
                throw new HttpRequestException("The server is away.");
        }
    }

    private static AngleSharp.Dom.IElement ChangesButton(IRenderedComponent<FilePreviewModal> component)
    {
        return component.Find("button[aria-label='Změny']");
    }

    private static async Task Pick(IRenderedComponent<FilePreviewModal> component, string title)
    {
        await component.Find(".ec-text-compare-version .ec-popover-trigger").ClickAsync(new MouseEventArgs());
        await component
            .FindAll(".ec-popover .ec-preview-versions-option")
            .Single(option => option.QuerySelector(".ec-preview-versions-option-title")!.TextContent.Trim().StartsWith(title, StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());
    }

    private static byte[][] Texts(params string[] texts)
    {
        return [.. texts.Select(static text => Encoding.UTF8.GetBytes(text))];
    }

    private static BunitContext NewContext(out StubTextEditors editors, out StubMarkdownEditors markdown, out StubTextCompares compares)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        editors = new StubTextEditors();
        markdown = new StubMarkdownEditors();
        compares = new StubTextCompares();
        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());
        ctx.Services.AddSingleton<ITextEditors>(editors);
        ctx.Services.AddSingleton<ITextCompares>(compares);
        ctx.Services.AddSingleton<IMarkdownEditors>(markdown);
        ctx.Services.AddSingleton(new StubBrowserHistory());
        ctx.Services.AddSingleton<IBrowserHistory>(static services => services.GetRequiredService<StubBrowserHistory>());
        ctx.Services.AddSingleton(Substitute.For<IFileSignaturesClient>());

        var signers = Substitute.For<IFileSignersClient>();
        signers.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [] });
        signers.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignersResponse>().Task);
        ctx.Services.AddSingleton(signers);

        return ctx;
    }

    // contents holds the text of each version, the oldest first; sizes their sizes, by default their lengths. The current
    // version is edited.
    private static async Task<IRenderedComponent<FilePreviewModal>> RenderEditing(
        BunitContext ctx,
        FileListItem file,
        byte[][] contents,
        long[]? sizes = null,
        Action<int?>? onRead = null)
    {
        IReadOnlyList<FileVersionItem> versions =
        [
            .. Enumerable.Range(1, contents.Length).Reverse().Select(number => new FileVersionItem
            {
                Number = number,
                SizeBytes = sizes?[number - 1] ?? contents[number - 1].Length,
                Created = new DateTime(2025, 9, number, 10, 0, 0, DateTimeKind.Utc),
                UploaderEmail = "jana@example.test",
            }),
        ];

        var component = ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, [file])
            .Add(static modal => modal.FileId, file.FileId)
            .Add(static modal => modal.LoadContent, Load)
            .Add(static modal => modal.LoadVersions, (_, _) => Task.FromResult(versions))
            .Add(static modal => modal.UploadVersion, static (_, _) => Task.FromResult<(string?, string?)>((null, null)))
            .Add(static modal => modal.Rename, static (_, _, _) => Task.CompletedTask)
            .Add(static modal => modal.Open, value: true));

        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());

        return component;

        Task<Stream> Load(FileListItem item, int? number, CancellationToken _)
        {
            onRead?.Invoke(number);

            return Task.FromResult<Stream>(new MemoryStream(contents[(number ?? item.Version) - 1]));
        }
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

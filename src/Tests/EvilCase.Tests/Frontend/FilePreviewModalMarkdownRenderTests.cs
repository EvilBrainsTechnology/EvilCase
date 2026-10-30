using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalMarkdownRenderTests
{
    private const string Original = "# Nadpis\n\nText\n";

    private const string LinkWindow = "dialog[aria-label='Odkaz']";

    private static readonly byte[] Mark = [0xEF, 0xBB, 0xBF];

    [Test]
    public void AFormattedViewThatDoesNotStartOffersTheDownload()
    {
        using var ctx = NewContext(out var markdown, out _, out _);

        markdown.Fails = true;

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"), "a file that cannot be shown offers the download"));
    }

    [Test]
    public async Task UpravitWhileTheZoomScriptLoadsStillEditsInPlace()
    {
        await using var ctx = NewContext(out var markdown, out _, out var previews);

        previews.TextViewLoading = new TaskCompletionSource();

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        await component.WaitForAssertionAsync(() => Assert.That(markdown.Opened, Has.Count.EqualTo(1)));
        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());
        await component.InvokeAsync(previews.TextViewLoading.SetResult);

        await component.WaitForAssertionAsync(() => Assert.That(markdown.Edits, Is.EqualTo(1), "the edit starts once the preview's toolbar is gone"));

        Assert.That(component.FindAll(".ec-markdown-toolbar"), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task SavingAChangedMarkdownKeepsTheMarkAndLineBreaks()
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        var uploaded = new List<byte[]>();
        var changed = new List<int?>();
        var file = File("Poznámky.md");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            [.. Mark, .. "# A\r\n\r\nb\r\n"u8],
            uploadVersion: async (_, picked) =>
            {
                uploaded.Add(await Read(picked));

                return (null, "Poznámky.md: nahrána verze 2.");
            },
            onVersionChanged: changed.Add);

        byte[] expected = [.. Mark, .. "# A\r\n\r\nc\r\n"u8];

        markdown.Text = "# A\n\nc\n";
        await Dirty(component);

        Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Is.EqualTo("Neuložené změny · upravujete verzi 1"));

        await SaveButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded.Single(), Is.EqualTo(expected), "the file keeps its mark and line breaks");
            Assert.That(markdown.Reads, Is.EqualTo(1), "the save reads the formatted editor");
            Assert.That(changed, Is.EqualTo(new int?[] { null }));
            Assert.That(component.FindAll(".ec-markdown-toolbar"), Is.Empty, "a save ends the edit");
        }
    }

    [Test]
    public async Task TheSwitchCarriesAFormattedChangeAcrossUnsaved()
    {
        await using var ctx = NewContext(out var markdown, out var editors, out _);

        var uploaded = new List<byte[]>();
        var file = File("Poznámky.md");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            "# A\n\nb\n"u8.ToArray(),
            uploadVersion: async (_, picked) =>
            {
                uploaded.Add(await Read(picked));

                return (null, null);
            });

        markdown.Text = "# A\n\nc\n";
        await Dirty(component);
        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Opened[^1].Text, Is.EqualTo("# A\n\nc\n"), "the change goes across");
            Assert.That(SaveButton(component).HasAttribute("disabled"), Is.False, "the change stays unsaved");
            Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Does.StartWith("Neuložené změny"));
        }

        editors.Text = "nečteno";
        await SaveButton(component).ClickAsync(new MouseEventArgs());

        Assert.That(Encoding.UTF8.GetString(uploaded.Single()), Is.EqualTo("# A\n\nc\n"), "an unchanged source view saves the text carried across");
    }

    [TestCase("javascript:alert(1)")]
    [TestCase("/spis/1")]
    [TestCase("ftp://example.cz")]
    [TestCase("https://")]
    public async Task TheLinkWindowRefusesAnAddressThatIsNotWebOrMail(string address)
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await ToolbarButton(component, "Odkaz").ClickAsync(new MouseEventArgs());
        await component.Find("#markdown-link-href").InputAsync(new ChangeEventArgs { Value = address });
        await LinkButton(component, "Vložit odkaz").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find($"{LinkWindow} .ec-field-error").TextContent, Is.EqualTo("Zadejte adresu, která začíná https://, http:// nebo mailto:."), "a link goes only to a web or e-mail address");
            Assert.That(markdown.Links, Is.Empty);
            Assert.That(component.FindAll(LinkWindow), Has.Count.EqualTo(1), "the window stays open");
        }
    }

    [Test]
    public async Task ClosingThePreviewClosesTheMarkdownView()
    {
        var ctx = NewContext(out var markdown, out _, out var previews);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        await component.WaitForAssertionAsync(() => Assert.That(previews.OpenedTextViews, Is.EqualTo(1)));

        await component.Instance.DisposeAsync();
        await ctx.DisposeAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(markdown.Closed, Is.EqualTo(1), "the formatted view closes with the preview");
            Assert.That(previews.DisposedViews, Is.EqualTo(1), "its zoom and search close with it");
        }
    }

    private static async Task Dirty(IRenderedComponent<FilePreviewModal> component)
    {
        await Changed(component, dirty: true, [], []);
    }

    private static async Task Changed(IRenderedComponent<FilePreviewModal> component, bool dirty, string[] active, string[] disabled)
    {
        await component.InvokeAsync(async () => await component.FindComponent<MarkdownView>().Instance.NotifyChanged(dirty, active, disabled));
    }

    private static async Task<byte[]> Read(IBrowserFile picked)
    {
        await using var stream = picked.OpenReadStream();
        await using var bytes = new MemoryStream();

        await stream.CopyToAsync(bytes);

        return bytes.ToArray();
    }

    private static AngleSharp.Dom.IElement ToolbarButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-markdown-toolbar button[aria-label='{label}']");
    }

    private static AngleSharp.Dom.IElement ModeButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-markdown-modes button[aria-label='{label}']");
    }

    private static AngleSharp.Dom.IElement LinkButton(IRenderedComponent<FilePreviewModal> component, string text)
    {
        return component
            .FindAll($"{LinkWindow} .ec-modal-footer button")
            .Single(button => string.Equals(button.TextContent.Trim(), text, StringComparison.Ordinal));
    }

    private static AngleSharp.Dom.IElement HeaderButton(IRenderedComponent<FilePreviewModal> component, string text)
    {
        return component
            .FindAll(".ec-modal-large .ec-modal-header button")
            .Single(button => string.Equals(button.TextContent.Trim(), text, StringComparison.Ordinal));
    }

    private static AngleSharp.Dom.IElement SaveButton(IRenderedComponent<FilePreviewModal> component)
    {
        return HeaderButton(component, "Uložit");
    }

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static BunitContext NewContext(out StubMarkdownEditors markdown, out StubTextEditors editors, out StubFilePreviews previews)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        markdown = new StubMarkdownEditors();
        editors = new StubTextEditors();
        previews = new StubFilePreviews();
        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());
        ctx.Services.AddSingleton<ITextEditors>(editors);
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

    private static async Task<IRenderedComponent<FilePreviewModal>> RenderEditing(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        byte[]? content = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<int?>? onVersionChanged = null)
    {
        var component = Render(ctx, files, fileId, content, uploadVersion, onVersionChanged);

        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());

        return component;
    }

    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        byte[]? content = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<int?>? onVersionChanged = null)
    {
        var bytes = content ?? Encoding.UTF8.GetBytes(Original);
        var upload = uploadVersion ?? (static (_, _) => Task.FromResult<(string?, string?)>((null, null)));

        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, (_, _, _) => Task.FromResult<Stream>(new MemoryStream(bytes)))
            .Add(static modal => modal.VersionChanged, changed => onVersionChanged?.Invoke(changed))
            .Add(static modal => modal.LoadVersions, static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]))
            .Add(static modal => modal.UploadVersion, upload)
            .Add(static modal => modal.Rename, static (_, _, _) => Task.CompletedTask)
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.OpenChanged, static _ => { }));
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

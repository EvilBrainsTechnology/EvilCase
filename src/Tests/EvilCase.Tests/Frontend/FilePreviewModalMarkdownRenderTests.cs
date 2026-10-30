using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalMarkdownRenderTests
{
    private const string Original = "# Nadpis\n\nText\n";

    private const string Label = "Obsah souboru Poznámky.md";

    [Test]
    public void AMarkdownFileOpensFormattedAndReadOnly()
    {
        using var ctx = NewContext(out var markdown, out var editors, out var previews);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId, "# Nadpis\n\nText"u8.ToArray());

        component.WaitForAssertion(() => Assert.That(previews.OpenedTextViews, Is.EqualTo(1), "the formatted preview has its zoom and search"));

        var documents = component.FindAll(".ec-preview-stage [role='document']");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(markdown.Opened, Is.EqualTo([("# Nadpis\n\nText", false, Label)]), "a Markdown file opens formatted and read-only");
            Assert.That(documents, Has.Count.EqualTo(1), "the zoom and search apply to the one document element");
            Assert.That(documents[0].Matches("article.ec-preview-markdown"), Is.True);
            Assert.That(documents[0].GetAttribute("tabindex"), Is.EqualTo("0"), "the read-only text takes the focus to scroll");
            Assert.That(editors.Opened, Is.Empty, "the source view opens only when chosen");
            Assert.That(component.FindAll(".ec-preview-line"), Is.Empty);
        }
    }

    [Test]
    public void TheMarkdownToolbarHasTheZoomTheSwitchAndTheSearch()
    {
        using var ctx = NewContext(out _, out _, out _);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                ToolbarLabels(component, ".ec-preview-toolbar"),
                Is.EqualTo(["Zmenšit", "Zvětšit", "100 %", "Formátovaně", "Zdrojový kód", "Hledat v dokumentu", "Předchozí výskyt", "Další výskyt"]),
                "the switch stands between the zoom and the search");
            Assert.That(ModeButton(component, "Formátovaně").GetAttribute("aria-pressed"), Is.EqualTo("true"), "a preview starts formatted");
            Assert.That(ModeButton(component, "Zdrojový kód").GetAttribute("aria-pressed"), Is.EqualTo("false"));
            Assert.That(component.Find(".ec-markdown-modes").GetAttribute("aria-label"), Is.EqualTo("Zobrazení"));
        }
    }

    [Test]
    public void ALongMarkdownIsCutAndSaysSo()
    {
        using var ctx = NewContext(out var markdown, out _, out _);

        var file = File("Velky.markdown");
        var component = Render(ctx, [file], file.FileId, Encoding.ASCII.GetBytes(new string('a', (1024 * 1024) + 10)));

        component.WaitForAssertion(() => Assert.That(markdown.Opened, Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-note").TextContent, Does.Contain("jen jeho začátek"), "a cut text says so");
            Assert.That(markdown.Opened[0].Text, Has.Length.EqualTo(1024 * 1024), "the preview shows at most the first 1 MB");
            Assert.That(component.FindAll("[aria-label='Upravit']"), Is.Empty, "only a file read whole is edited");
        }
    }

    [Test]
    public async Task TheSourceViewShowsTheTextReadOnlyWithItsSearch()
    {
        await using var ctx = NewContext(out var markdown, out var editors, out var previews);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Opened.Select(static opened => (opened.Text, opened.Language, opened.Label)), Is.EqualTo([(Original, (CodeLanguage?)CodeLanguage.Markdown, Label)]), "the source is the text as read, highlighted as Markdown");
            Assert.That(editors.OpenedReadOnly, Is.EqualTo([true]), "the source preview takes no input");
            Assert.That(markdown.Closed, Is.EqualTo(1), "the formatted view closes");
            Assert.That(previews.ViewZooms, Is.EqualTo([PreviewZoom.Actual]), "the text goes back to its true size");
            Assert.That(previews.DisposedViews, Is.EqualTo(1), "the source view has no zoom");
            Assert.That(component.Find(".ec-text-editor-toolbar").GetAttribute("aria-label"), Is.EqualTo("Ovládání náhledu"));
            Assert.That(component.FindAll("button[aria-label='Zpět'], button[aria-label='Znovu']"), Is.Empty, "a preview has no undo");
            Assert.That(ModeButton(component, "Zdrojový kód").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(component.FindAll("input[aria-label='Hledat v textu']"), Has.Count.EqualTo(1), "the source view searches like the editor");
        }
    }

    [Test]
    public async Task TheChosenViewHoldsForTheNextMarkdownFile()
    {
        await using var ctx = NewContext(out var markdown, out var editors, out _);

        var first = File("Poznámky.md");
        var second = File("Další.md");
        var component = Render(ctx, [first, second], first.FileId);

        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());
        component.Render(parameters => parameters.Add(static modal => modal.FileId, second.FileId));

        await component.WaitForAssertionAsync(() => Assert.That(editors.Opened, Has.Count.EqualTo(2), "the next Markdown file opens as its source"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Opened[1].Label, Is.EqualTo("Obsah souboru Další.md"));
            Assert.That(markdown.Opened, Has.Count.EqualTo(1), "only the first file opened formatted");
        }
    }

    [Test]
    public async Task SwitchingBackOpensTheFormattedViewAgain()
    {
        await using var ctx = NewContext(out var markdown, out var editors, out var previews);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());
        await ModeButton(component, "Formátovaně").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(previews.OpenedTextViews, Is.EqualTo(2), "the formatted view gets its zoom and search back"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(markdown.Opened, Is.EqualTo([(Original, false, Label), (Original, false, Label)]), "the formatted view opens again read-only");
            Assert.That(editors.Closed, Is.EqualTo(1), "the source view closes");
        }
    }

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

    private static List<string?> ToolbarLabels(IRenderedComponent<FilePreviewModal> component, string selector)
    {
        return [.. component.FindAll($"{selector} button").Select(static button => button.GetAttribute("aria-label"))];
    }

    private static AngleSharp.Dom.IElement ModeButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-markdown-modes button[aria-label='{label}']");
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

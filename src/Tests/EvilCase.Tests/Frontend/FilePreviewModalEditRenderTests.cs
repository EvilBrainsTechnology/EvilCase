using Bunit;
using Bunit.TestDoubles;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalEditRenderTests
{
    private const string DiscardQuestion = "dialog[aria-label='Zahodit neuložené změny?']";

    private static readonly byte[] Mark = [0xEF, 0xBB, 0xBF];

    [Test]
    public void AnOlderVersionOffersNoEditing()
    {
        using var ctx = NewContext(out _);

        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, "a"u8.ToArray(), version: 2);

        Assert.That(component.FindAll("[aria-label='Upravit']"), Is.Empty);
    }

    [Test]
    public async Task SavingUploadsTheTextAsANewVersionWithTheFilesLineBreaksAndMark()
    {
        await using var ctx = NewContext(out var editors);

        var uploaded = new List<(string Name, byte[] Content)>();
        var changed = new List<int?>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            [.. Mark, .. "a\r\nb\r\n"u8],
            uploadVersion: async (_, picked) =>
            {
                await using var stream = picked.OpenReadStream();
                await using var bytes = new MemoryStream();

                await stream.CopyToAsync(bytes);
                uploaded.Add((picked.Name, bytes.ToArray()));

                return (null, "Poznámky.txt: nahrána verze 2.");
            },
            onVersionChanged: changed.Add);

        byte[] expected = [.. Mark, .. "a\r\nc\r\n"u8];

        editors.Text = "a\nc\n";
        await Dirty(component);
        await SaveButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded.Select(static upload => upload.Name), Is.EqualTo(["Poznámky.txt"]));
            Assert.That(uploaded.Single().Content, Is.EqualTo(expected), "the file keeps its mark and line breaks");
            Assert.That(changed, Is.EqualTo(new int?[] { null }));
            Assert.That(component.FindComponent<TextEditor>().Instance.ReadOnly, Is.True, "the preview shows the text read-only again");
        }
    }

    [Test]
    public async Task CtrlSSavesOnlyAChangedText()
    {
        await using var ctx = NewContext(out _);

        var uploads = 0;
        var file = File("Poznámky.txt");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            uploadVersion: (_, _) =>
            {
                uploads++;

                return Task.FromResult<(string?, string?)>(("Poznámky.txt: nahrání se nezdařilo.", null));
            });
        var editor = component.FindComponent<TextEditor>().Instance;

        await component.InvokeAsync(editor.NotifySave);

        Assert.That(uploads, Is.Zero, "an unchanged text is not saved");

        await Dirty(component);
        await component.InvokeAsync(editor.NotifySave);

        Assert.That(uploads, Is.EqualTo(1));
    }

    [Test]
    public async Task AFailedSaveKeepsTheTextAndOffersRetry()
    {
        await using var ctx = NewContext(out _);

        var uploads = 0;
        var changed = new List<int?>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            uploadVersion: (_, _) =>
            {
                uploads++;

                return Task.FromResult<(string?, string?)>(("Poznámky.txt: nahrání se nezdařilo.", null));
            },
            onVersionChanged: changed.Add);

        await Dirty(component);
        await SaveButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindComponent<TextEditor>().Instance.ReadOnly, Is.False, "a failed save keeps the text");
            Assert.That(changed, Is.Empty);
        }

        await component
            .FindAll(".ec-preview-alert button")
            .Single(static button => button.TextContent.Contains("Zkusit znovu", StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        Assert.That(uploads, Is.EqualTo(2));
    }

    [Test]
    public async Task ASaveUnderWayTakesNoInputAndCannotBeCancelled()
    {
        await using var ctx = NewContext(out _);

        var upload = new TaskCompletionSource<(string?, string?)>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId, uploadVersion: async (_, _) => await upload.Task);

        await Dirty(component);

        var saving = SaveButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-text-editor-stage").HasAttribute("inert"), Is.True);
            Assert.That(HeaderButton(component, "Zrušit").HasAttribute("disabled"), Is.True);
        }

        await component.Find(".ec-modal-large .ec-modal-header button[aria-label='Zavřít']").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(DiscardQuestion), Is.Empty, "the save finishes first");

        upload.SetResult(("Poznámky.txt: nahrání se nezdařilo.", null));
        await saving;

        Assert.That(component.Find(".ec-text-editor-stage").HasAttribute("inert"), Is.False);
    }

    [Test]
    public async Task AnUnreadableEditorFailsTheSaveAndKeepsTheText()
    {
        await using var ctx = NewContext(out var editors);

        var uploads = 0;
        var file = File("Poznámky.txt");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            uploadVersion: (_, _) =>
            {
                uploads++;

                return Task.FromResult<(string?, string?)>((null, null));
            });

        editors.ReadFails = true;
        await Dirty(component);
        await SaveButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploads, Is.Zero);
            Assert.That(component.FindComponent<TextEditor>().Instance.ReadOnly, Is.False, "the editor stays");
        }
    }

    [Test]
    public async Task AVersionThatArrivesWhileEditingKeepsTheEditorAndShowsAfterIt()
    {
        await using var ctx = NewContext(out _);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);
        component.Render(parameters => parameters.Add(static modal => modal.Files, [file with { Version = 2, VersionCount = 2 }]));

        Assert.That(component.FindComponent<TextEditor>().Instance.ReadOnly, Is.False, "the unsaved text stays");

        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());
        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-actions button[aria-label='Verze, 2']"), Has.Count.EqualTo(1)));
    }

    [Test]
    public async Task CancelWithChangesAsksAndDiscardReturnsToTheText()
    {
        await using var ctx = NewContext(out _);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);
        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(DiscardQuestion), Has.Count.EqualTo(1), "unsaved changes are confirmed first");

        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(DiscardQuestion), Is.Empty);
            Assert.That(component.FindComponent<TextEditor>().Instance.ReadOnly, Is.True, "the preview shows the text read-only again");
        }
    }

    [Test]
    public async Task ClosingWithChangesAsksFirstAndClosesAfterDiscard()
    {
        await using var ctx = NewContext(out _);

        var raised = new List<bool>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId, onOpenChanged: raised.Add);

        await Dirty(component);
        await component.Find(".ec-modal-large .ec-modal-header button[aria-label='Zavřít']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(raised, Is.Empty, "unsaved changes are confirmed first");
            Assert.That(component.FindAll(DiscardQuestion), Has.Count.EqualTo(1));
        }

        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        Assert.That(raised, Is.EqualTo([false]));
    }

    [Test]
    public async Task LeavingThePageWithChangesAsksAndGoesOnAfterDiscard()
    {
        await using var ctx = NewContext(out _);

        var navigation = ctx.Services.GetRequiredService<BunitNavigationManager>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);

        Assert.That(component.FindComponent<NavigationLock>().Instance.ConfirmExternalNavigation, Is.True, "the browser asks before leaving the site");

        await component.InvokeAsync(() => navigation.NavigateTo("/cases"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(navigation.History.First().State, Is.EqualTo(NavigationState.Prevented));
            Assert.That(component.FindAll(DiscardQuestion), Has.Count.EqualTo(1));
        }

        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        Assert.That(navigation.Uri, Does.EndWith("/cases"));
    }

    [Test]
    public async Task BackAfterDiscardTakesTheHistoryStepInsteadOfANewEntry()
    {
        await using var ctx = NewContext(out _);

        var navigation = ctx.Services.GetRequiredService<BunitNavigationManager>();
        var history = ctx.Services.GetRequiredService<StubBrowserHistory>();
        history.Distance = -1;
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);
        var start = navigation.Uri;

        await Dirty(component);
        await component.InvokeAsync(() => navigation.NavigateTo("/cases"));
        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(history.Tracked, Is.EqualTo(1));
            Assert.That(history.Taken.Single(), Does.EndWith("/cases"));
            Assert.That(history.Moves, Is.EqualTo([-1]), "the browser takes the step again");
            Assert.That(navigation.Uri, Is.EqualTo(start), "the app adds no entry");
            Assert.That(navigation.History, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task AnEditorThatDoesNotStartReturnsToTheText()
    {
        await using var ctx = NewContext(out var editors);

        editors.Fails = true;

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        Assert.That(component.FindComponent<TextEditor>().Instance.ReadOnly, Is.True, "the preview shows the text read-only again");
    }

    [Test]
    public async Task ClosingThePreviewClosesTheEditor()
    {
        var ctx = NewContext(out var editors);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await component.Instance.DisposeAsync();
        await ctx.DisposeAsync();

        Assert.That(editors.Closed, Is.EqualTo(2), "the view closes for the edit and the editor with the preview");
    }

    [Test]
    public async Task UpravitOnFormattedCodeEditsTheOriginal()
    {
        await using var ctx = NewContext(out var editors);

        var file = File("data.json");
        await RenderEditing(ctx, [file], file.FileId, "{\"a\":1}"u8.ToArray());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Formatting, Is.EqualTo([true]), "only the preview formats");
            Assert.That(editors.OpenedReadOnly, Is.EqualTo([true, false]));
            Assert.That(editors.Opened[^1].Text, Is.EqualTo("{\"a\":1}"), "the edit starts from the original, never the formatted text");
        }
    }

    private static async Task Dirty(IRenderedComponent<FilePreviewModal> component)
    {
        await component.InvokeAsync(async () => await component.FindComponent<TextEditor>().Instance.NotifyChanged(dirty: true, canUndo: true, canRedo: false));
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

    private static AngleSharp.Dom.IElement QuestionButton(IRenderedComponent<FilePreviewModal> component, string text)
    {
        return component
            .FindAll($"{DiscardQuestion} .ec-modal-footer button")
            .Single(button => string.Equals(button.TextContent.Trim(), text, StringComparison.Ordinal));
    }

    private static FileListItem Versioned(string fileName, int version, int count)
    {
        return File(fileName) with { Version = version, VersionCount = count };
    }

    private static BunitContext NewContext(out StubTextEditors editors)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        editors = new StubTextEditors();
        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());
        ctx.Services.AddSingleton<IFileDownloader>(new StubFileDownloader());
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());
        ctx.Services.AddSingleton<ITextEditors>(editors);
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

    private static async Task<IRenderedComponent<FilePreviewModal>> RenderEditing(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        byte[]? content = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<int?>? onVersionChanged = null,
        Action<bool>? onOpenChanged = null)
    {
        var component = Render(ctx, files, fileId, content ?? "a\nb\n"u8.ToArray(), uploadVersion: uploadVersion, onVersionChanged: onVersionChanged, onOpenChanged: onOpenChanged);

        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());

        return component;
    }

    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        byte[] content,
        int? version = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<int?>? onVersionChanged = null,
        Action<bool>? onOpenChanged = null)
    {
        var upload = uploadVersion ?? (static (_, _) => Task.FromResult<(string?, string?)>((null, null)));

        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, (_, _, _) => Task.FromResult<Stream>(new MemoryStream(content)))
            .Add(static modal => modal.Version, version)
            .Add(static modal => modal.VersionChanged, changed => onVersionChanged?.Invoke(changed))
            .Add(static modal => modal.LoadVersions, static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]))
            .Add(static modal => modal.UploadVersion, upload)
            .Add(static modal => modal.Rename, static (_, _, _) => Task.CompletedTask)
            .Add(static modal => modal.Open, value: true)
            .Add(static modal => modal.OpenChanged, open => onOpenChanged?.Invoke(open)));
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

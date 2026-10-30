using System.Text;
using Bunit;
using Bunit.TestDoubles;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Components.Ec;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalEditRenderTests
{
    private const string NotUtf8Note = "Soubor není v kódování UTF-8, proto ho nejde upravit.";

    private const string DiscardQuestion = "dialog[aria-label='Zahodit neuložené změny?']";

    private static readonly byte[] Mark = [0xEF, 0xBB, 0xBF];

    [TestCase("a.txt")]
    [TestCase("a.csv")]
    [TestCase("a.tsv")]
    [TestCase("a.log")]
    [TestCase("a.json")]
    [TestCase("a.xml")]
    [TestCase("a.isdoc")]
    [TestCase("a.yaml")]
    [TestCase("a.yml")]
    [TestCase("a.html")]
    [TestCase("a.md")]
    [TestCase("a.markdown")]
    public void AnEditableFileOffersUpravitBeforeTheVersions(string fileName)
    {
        using var ctx = NewContext(out _);

        var file = Versioned(fileName, 3, 3);
        var component = Render(ctx, [file], file.FileId, "{}"u8.ToArray());

        var labels = component
            .FindAll(".ec-preview-actions button")
            .Select(static button => button.GetAttribute("aria-label"))
            .ToList();
        var edit = labels.IndexOf("Upravit");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(edit, Is.GreaterThanOrEqualTo(0));
            Assert.That(labels.ElementAtOrDefault(edit + 1), Is.EqualTo("Verze, 3"), "Upravit stands right before Verze");
        }
    }

    [Test]
    public void AnOlderVersionOffersNoEditing()
    {
        using var ctx = NewContext(out _);

        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, "a"u8.ToArray(), version: 2);

        Assert.That(component.FindAll("[aria-label='Upravit']"), Is.Empty);
    }

    [Test]
    public void AFileLongerThanTheReadLimitOffersNoEditing()
    {
        using var ctx = NewContext(out _);

        var file = File("a.txt");
        var component = Render(ctx, [file], file.FileId, Encoding.ASCII.GetBytes(new string('a', (1024 * 1024) + 1)));

        Assert.That(component.FindAll("[aria-label='Upravit']"), Is.Empty, "only a file read whole is edited");
    }

    [Test]
    public void ATextWithMoreLinesThanTheViewShowsIsStillEditable()
    {
        using var ctx = NewContext(out _);

        var file = File("a.txt");
        var component = Render(ctx, [file], file.FileId, Encoding.ASCII.GetBytes(string.Concat(Enumerable.Repeat("a\n", 6000))));

        Assert.That(component.FindAll("[aria-label='Upravit']"), Has.Count.EqualTo(1));
    }

    [TestCase("a.txt")]
    [TestCase("a.json")]
    [TestCase("a.md")]
    public void AFileThatIsNotUtf8OffersNoEditingAndSaysWhy(string fileName)
    {
        using var ctx = NewContext(out _);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId, [0x50, 0xF8]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("[aria-label='Upravit']"), Is.Empty);
            Assert.That(component.FindAll(".ec-preview-note").Select(static note => note.TextContent), Does.Contain(NotUtf8Note));
        }
    }

    [Test]
    public void ANonUtf8TextWithMoreLinesThanTheViewShowsSaysWhy()
    {
        using var ctx = NewContext(out _);

        var file = File("a.log");
        var component = Render(ctx, [file], file.FileId, [.. Enumerable.Repeat<byte>(0x0A, 6000), 0xF8]);

        Assert.That(component.FindAll(".ec-preview-note").Select(static note => note.TextContent), Does.Contain(NotUtf8Note));
    }

    [TestCase("data.tsv", "a\n  b\n", "\t")]
    [TestCase("data.json", "{}", "\t")]
    [TestCase("data.json", "{\n\t\n  \"a\": 1\n}", "  ")]
    [TestCase("data.xml", "<a>\r\n    <b/>\r\n  <c/>\r\n</a>", "    ")]
    [TestCase("data.xml", "<a>\n\t<b/>\n</a>", "\t")]
    [TestCase("data.html", "<p>\n            a</p>", "        ")]
    [TestCase("data.yaml", "a:\n  b: c\n", "  ")]
    [TestCase("data.yaml", "a: 1\nb: 2\n", "  ")]
    public async Task TabTypesTheIndentationOfCodeAndATabInText(string fileName, string text, string indentUnit)
    {
        await using var ctx = NewContext(out var editors);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId, Encoding.UTF8.GetBytes(text));

        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());

        Assert.That(editors.Opened.Single().IndentUnit, Is.EqualTo(indentUnit));
    }

    [TestCase("data.json", CodeLanguage.Json)]
    [TestCase("Poznámky.txt", null)]
    public async Task UpravitOpensTheEditorWithTheTextAndItsLanguage(string fileName, CodeLanguage? language)
    {
        await using var ctx = NewContext(out var editors);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId, [.. Mark, .. "{}"u8]);

        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Opened, Is.EqualTo([("{}", language, "\t", $"Obsah souboru {fileName}")]));
            Assert.That(HeaderButtons(component), Is.EqualTo(["Zrušit", "Uložit", "Zavřít"]));
            Assert.That(SaveButton(component).HasAttribute("disabled"), Is.True, "nothing changed yet");
            Assert.That(component.FindAll(".ec-preview-nav"), Is.Empty);
            Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Is.EqualTo("Upravujete verzi 1"));
            Assert.That(component.Find("button[aria-label='Zpět']").HasAttribute("disabled"), Is.True);
            Assert.That(component.Find("button[aria-label='Znovu']").HasAttribute("disabled"), Is.True);
            Assert.That(component.Find(".ec-text-editor-toolbar input[type=search]").GetAttribute("placeholder"), Is.EqualTo("Hledat v textu"));
        }
    }

    [Test]
    public async Task AChangeMarksTheHeaderAndEnablesSaveAndUndo()
    {
        await using var ctx = NewContext(out _);

        var file = Versioned("Poznámky.txt", 3, 3);
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Is.EqualTo("Neuložené změny · upravujete verzi 3"));
            Assert.That(SaveButton(component).HasAttribute("disabled"), Is.False);
            Assert.That(component.Find("button[aria-label='Zpět']").HasAttribute("disabled"), Is.False);
            Assert.That(component.Find("button[aria-label='Znovu']").HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public async Task UndoAndRedoGoToTheEditor()
    {
        await using var ctx = NewContext(out var editors);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await component.InvokeAsync(async () => await component.FindComponent<TextEditor>().Instance.NotifyChanged(dirty: true, canUndo: true, canRedo: true));
        await component.Find("button[aria-label='Zpět']").ClickAsync(new MouseEventArgs());
        await component.Find("button[aria-label='Znovu']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Undos, Is.EqualTo(1));
            Assert.That(editors.Redos, Is.EqualTo(1));
        }
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
            Assert.That(component.FindAll(".ec-text-editor"), Is.Empty);
            Assert.That(component.Find(".ec-file-notice").TextContent, Is.EqualTo("Poznámky.txt: nahrána verze 2."));
        }
    }

    [Test]
    public async Task AnUnchangedSaveReturnsToTheText()
    {
        await using var ctx = NewContext(out _);

        var changed = new List<int?>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            uploadVersion: static (_, _) => Task.FromResult<(string?, string?)>((null, "Poznámky.txt: obsah je stejný jako poslední verze.")),
            onVersionChanged: changed.Add);

        await Dirty(component);
        await SaveButton(component).ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-text-editor"), Is.Empty);
            Assert.That(changed, Is.EqualTo(new int?[] { null }));
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
            Assert.That(component.Find(".ec-preview-alert .ec-alert-text").TextContent, Is.EqualTo("Poznámky.txt: nahrání se nezdařilo."));
            Assert.That(component.FindAll(".ec-text-editor"), Has.Count.EqualTo(1), "a failed save keeps the text");
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
            Assert.That(component.Find(".ec-preview-alert .ec-alert-text").TextContent, Is.EqualTo("Text se nepodařilo přečíst z editoru. Zkuste to znovu."));
            Assert.That(component.FindAll(".ec-text-editor"), Has.Count.EqualTo(1));
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

        Assert.That(component.FindAll(".ec-text-editor"), Has.Count.EqualTo(1), "the unsaved text stays");

        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());
        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-actions button[aria-label='Verze, 2']"), Has.Count.EqualTo(1)));
    }

    [Test]
    public async Task CancelWithoutChangesReturnsToTheTextAtOnce()
    {
        await using var ctx = NewContext(out _);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(DiscardQuestion), Is.Empty);
            Assert.That(component.FindAll(".ec-text-editor"), Is.Empty);
            Assert.That(component.FindAll("[aria-label='Upravit']"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task CancelWithChangesAsksAndDiscardReturnsToTheText()
    {
        await using var ctx = NewContext(out _);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);
        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());

        var question = component.Find(DiscardQuestion);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(question.QuerySelector(".ec-confirm-text")!.TextContent, Is.EqualTo("Úpravy souboru Poznámky.txt nejsou uložené. Po zahození se nedají vrátit."));
            Assert.That(question.QuerySelectorAll(".ec-modal-footer button").Select(static button => button.TextContent.Trim()), Is.EqualTo(["Pokračovat v úpravách", "Zahodit změny"]));
        }

        await QuestionButton(component, "Zahodit změny").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(DiscardQuestion), Is.Empty);
            Assert.That(component.FindAll(".ec-text-editor"), Is.Empty);
        }
    }

    [Test]
    public async Task KeepEditingClosesTheQuestionAndKeepsTheChanges()
    {
        await using var ctx = NewContext(out _);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);
        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());
        await QuestionButton(component, "Pokračovat v úpravách").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(DiscardQuestion), Is.Empty);
            Assert.That(component.FindAll(".ec-text-editor"), Has.Count.EqualTo(1));
            Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Does.StartWith("Neuložené změny"));
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
    public async Task EscapeWithChangesAsksFirst()
    {
        await using var ctx = NewContext(out _);

        var raised = new List<bool>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId, onOpenChanged: raised.Add);

        await Dirty(component);

        var preview = component.FindComponents<EcModal>()[0];

        await preview.InvokeAsync(preview.Instance.NotifyCloseRequested);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(DiscardQuestion), Has.Count.EqualTo(1));
            Assert.That(raised, Is.Empty, "unsaved changes are confirmed first");
        }
    }

    [Test]
    public async Task ClosingWithoutChangesWhileEditingClosesAtOnce()
    {
        await using var ctx = NewContext(out _);

        var raised = new List<bool>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId, onOpenChanged: raised.Add);

        await component.Find(".ec-modal-large .ec-modal-header button[aria-label='Zavřít']").ClickAsync(new MouseEventArgs());

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
    public async Task LeavingThePageWithoutChangesGoesOn()
    {
        await using var ctx = NewContext(out _);

        var navigation = ctx.Services.GetRequiredService<BunitNavigationManager>();
        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await component.InvokeAsync(() => navigation.NavigateTo("/cases"));

        Assert.That(navigation.Uri, Does.EndWith("/cases"));
    }

    [Test]
    public async Task AFileWithADetachedSignatureWarnsThatTheNewVersionInvalidatesIt()
    {
        await using var ctx = NewContext(out _);

        var signature = File("Data.xml.p7s");
        var file = File("Data.xml") with { SignatureFileId = signature.FileId };
        var component = await RenderEditing(ctx, [file, signature], file.FileId, "<a/>"u8.ToArray());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-text-editor-note").TextContent, Is.EqualTo("Soubor má samostatný podpis Data.xml.p7s. Nová verze ho zneplatní."));
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty);
        }
    }

    [Test]
    public async Task AnEditorThatDoesNotStartReturnsToTheTextAndSaysSo()
    {
        await using var ctx = NewContext(out var editors);

        editors.Fails = true;

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-alert").TextContent, Is.EqualTo("Úpravy se nepodařilo spustit. Zkuste to za chvíli znovu."));
            Assert.That(component.FindAll(".ec-text-editor"), Is.Empty);
        }
    }

    [Test]
    public async Task ClosingThePreviewClosesTheEditor()
    {
        var ctx = NewContext(out var editors);

        var file = File("Poznámky.txt");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await component.Instance.DisposeAsync();
        await ctx.DisposeAsync();

        Assert.That(editors.Closed, Is.EqualTo(1));
    }

    private static async Task Dirty(IRenderedComponent<FilePreviewModal> component)
    {
        await component.InvokeAsync(async () => await component.FindComponent<TextEditor>().Instance.NotifyChanged(dirty: true, canUndo: true, canRedo: false));
    }

    private static List<string> HeaderButtons(IRenderedComponent<FilePreviewModal> component)
    {
        return
        [
            .. component
                .FindAll(".ec-modal-large .ec-modal-header button")
                .Select(static button => button.GetAttribute("aria-label") ?? button.TextContent.Trim()),
        ];
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

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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

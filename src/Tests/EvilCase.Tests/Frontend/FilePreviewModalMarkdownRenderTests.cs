using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalMarkdownRenderTests
{
    private const string Original = "# Nadpis\n\nText\n";

    private const string Label = "Obsah souboru Poznámky.md";

    private const string DiscardQuestion = "dialog[aria-label='Zahodit neuložené změny?']";

    private const string LinkWindow = "dialog[aria-label='Odkaz']";

    private static readonly byte[] Mark = [0xEF, 0xBB, 0xBF];

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
    public async Task TheSourceViewShowsTheTextReadOnlyWithTheToolbarOfThePreview()
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
            Assert.That(previews.ViewZooms, Is.EqualTo([PreviewZoom.Actual]), "the formatted view goes back to its true size");
            Assert.That(previews.DisposedViews, Is.EqualTo(1), "the formatted view's zoom and search end");
            Assert.That(editors.Formatting, Is.EqualTo([false]), "Markdown source is never formatted");
            Assert.That(
                ToolbarLabels(component, ".ec-preview-toolbar"),
                Is.EqualTo(["Zmenšit", "Zvětšit", "100 %", "Formátovaně", "Zdrojový kód", "Hledat v dokumentu", "Předchozí výskyt", "Další výskyt"]),
                "the source view has the toolbar of the formatted view");
            Assert.That(component.FindAll("button[aria-label='Zpět'], button[aria-label='Znovu']"), Is.Empty, "a preview has no undo");
            Assert.That(ModeButton(component, "Zdrojový kód").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        }
    }

    [Test]
    public async Task TheSourceViewZoomsAndSearchesInItsOwnView()
    {
        await using var ctx = NewContext(out _, out var editors, out var previews);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-toolbar button[aria-label='Zvětšit']").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-toolbar-search-input").InputAsync(new() { Value = "nadpis" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Zooms, Is.EqualTo([PreviewZoom.In]));
            Assert.That(editors.Finds, Is.EqualTo([("nadpis", PreviewFind.New)]));
            Assert.That(previews.ViewFinds, Is.Empty, "the formatted view's search is gone");
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
    public async Task UpravitEditsTheFormattedViewInPlace()
    {
        await using var ctx = NewContext(out var markdown, out _, out var previews);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await component.WaitForAssertionAsync(() => Assert.That(markdown.Edits, Is.EqualTo(1), "the shown text turns editable"));

        var article = component.Find("article.ec-preview-markdown");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(markdown.Opened, Has.Count.EqualTo(1), "the formatted view is not opened again");
            Assert.That(previews.ViewZooms, Is.EqualTo([PreviewZoom.Actual]), "the edit starts at 100 %");
            Assert.That(previews.DisposedViews, Is.EqualTo(1), "the editor has no zoom and search");
            Assert.That(component.Find(".ec-markdown-toolbar").GetAttribute("aria-label"), Is.EqualTo("Úpravy"));
            Assert.That(
                ToolbarLabels(component, ".ec-markdown-toolbar"),
                Is.EqualTo(["Zpět", "Znovu", "Formátovaně", "Zdrojový kód", "Nadpis 1", "Nadpis 2", "Nadpis 3", "Tučně", "Kurzíva", "Přeškrtnout", "Odrážky", "Číslovaný seznam", "Seznam úkolů", "Citace", "Kód", "Odkaz", "Tabulka"]));
            Assert.That(component.Find(".ec-markdown-modes").GetAttribute("aria-label"), Is.EqualTo("Režim úprav"));
            Assert.That(article.ClassList, Does.Contain("ec-markdown-editing"));
            Assert.That(article.HasAttribute("tabindex"), Is.False, "the editable text takes the focus itself");
            Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Is.EqualTo("Upravujete verzi 1"));
            Assert.That(SaveButton(component).HasAttribute("disabled"), Is.True, "nothing changed yet");
            Assert.That(ToolbarButton(component, "Zpět").HasAttribute("disabled"), Is.True);
            Assert.That(ToolbarButton(component, "Znovu").HasAttribute("disabled"), Is.True);
        }
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
    public async Task TheToolbarShowsWhatTheEditorReports()
    {
        await using var ctx = NewContext(out _, out _, out _);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Changed(component, dirty: false, ["strong", "heading2"], ["table", "redo"]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ToolbarButton(component, "Tučně").GetAttribute("aria-pressed"), Is.EqualTo("true"), "the format at the cursor is pressed");
            Assert.That(ToolbarButton(component, "Nadpis 2").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(ToolbarButton(component, "Kurzíva").GetAttribute("aria-pressed"), Is.EqualTo("false"));
            Assert.That(ToolbarButton(component, "Tabulka").HasAttribute("disabled"), Is.True, "a command unavailable at the cursor is disabled");
            Assert.That(ToolbarButton(component, "Tabulka").HasAttribute("aria-pressed"), Is.False, "Tabulka only inserts");
            Assert.That(ToolbarButton(component, "Znovu").HasAttribute("disabled"), Is.True);
            Assert.That(ToolbarButton(component, "Zpět").HasAttribute("disabled"), Is.False);
            Assert.That(SaveButton(component).HasAttribute("disabled"), Is.True, "a moved cursor is no change");
        }
    }

    [Test]
    public async Task ToolbarButtonsRunTheirCommands()
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Changed(component, dirty: false, [], []);

        foreach (var label in new[] { "Zpět", "Znovu", "Nadpis 1", "Tučně", "Přeškrtnout", "Seznam úkolů", "Citace", "Kód", "Tabulka" })
            await ToolbarButton(component, label).ClickAsync(new MouseEventArgs());

        Assert.That(markdown.Commands, Is.EqualTo(["undo", "redo", "heading1", "strong", "strike", "taskList", "quote", "code", "table"]), "each button runs its command");
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
            Assert.That(component.Find(".ec-file-notice").TextContent, Is.EqualTo("Poznámky.md: nahrána verze 2."));
            Assert.That(component.FindAll(".ec-markdown-toolbar"), Is.Empty, "a save ends the edit");
        }
    }

    [Test]
    public async Task AnUnchangedMarkdownEditCannotBeSaved()
    {
        await using var ctx = NewContext(out _, out _, out _);

        var uploads = 0;
        var file = File("Poznámky.md");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            uploadVersion: (_, _) =>
            {
                uploads++;

                return Task.FromResult<(string?, string?)>((null, null));
            });

        await Changed(component, dirty: false, ["strong"], []);

        Assert.That(SaveButton(component).HasAttribute("disabled"), Is.True, "an unchanged text cannot be saved");

        await component.InvokeAsync(component.FindComponent<MarkdownView>().Instance.NotifySave);

        Assert.That(uploads, Is.Zero, "Ctrl+S saves nothing unchanged");
    }

    [Test]
    public async Task CtrlSSavesAChangedMarkdown()
    {
        await using var ctx = NewContext(out _, out _, out _);

        var uploads = 0;
        var file = File("Poznámky.md");
        var component = await RenderEditing(
            ctx,
            [file],
            file.FileId,
            uploadVersion: (_, _) =>
            {
                uploads++;

                return Task.FromResult<(string?, string?)>((null, null));
            });

        await Dirty(component);
        await component.InvokeAsync(component.FindComponent<MarkdownView>().Instance.NotifySave);

        Assert.That(uploads, Is.EqualTo(1), "Ctrl+S saves a changed text");
    }

    [Test]
    public async Task CancelWithChangesAsksAndDiscardShowsTheTextAsRead()
    {
        await using var ctx = NewContext(out var markdown, out _, out var previews);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Dirty(component);
        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());
        await component
            .FindAll($"{DiscardQuestion} .ec-modal-footer button")
            .Single(static button => string.Equals(button.TextContent.Trim(), "Zahodit změny", StringComparison.Ordinal))
            .ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(previews.OpenedTextViews, Is.EqualTo(2), "the preview gets its zoom and search back"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(markdown.Shown, Is.EqualTo([Original]), "a discard shows the text as read");
            Assert.That(markdown.Opened, Has.Count.EqualTo(1), "the formatted view is not opened again");
            Assert.That(component.FindAll(".ec-markdown-toolbar"), Is.Empty);
        }
    }

    [Test]
    public async Task CancelWithoutChangesShowsTheTextAtOnce()
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await HeaderButton(component, "Zrušit").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(markdown.Shown, Is.EqualTo([Original]), "the edit ends with the text as read"));

        Assert.That(component.FindAll(DiscardQuestion), Is.Empty, "nothing changed, nothing is asked");
    }

    [Test]
    public async Task TheSwitchCarriesAnUnchangedTextAcrossClean()
    {
        await using var ctx = NewContext(out var markdown, out var editors, out _);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.Opened[^1].Text, Is.EqualTo(Original), "the source editor opens with the text as read");
            Assert.That(editors.OpenedReadOnly, Is.EqualTo([false]), "the source is edited");
            Assert.That(markdown.Reads, Is.Zero, "an unchanged text is not read back");
            Assert.That(markdown.Closed, Is.EqualTo(1), "the formatted editor closes");
            Assert.That(SaveButton(component).HasAttribute("disabled"), Is.True, "a switch is no change");
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

    [Test]
    public async Task UpravitInTheSourceViewEditsTheSourceAndItCarriesBack()
    {
        await using var ctx = NewContext(out var markdown, out var editors, out _);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId);

        await ModeButton(component, "Zdrojový kód").ClickAsync(new MouseEventArgs());
        await component.Find("button[aria-label='Upravit']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(editors.OpenedReadOnly, Is.EqualTo([true, false]), "Upravit edits the source view shown");
            Assert.That(component.Find(".ec-text-editor-toolbar").GetAttribute("aria-label"), Is.EqualTo("Úpravy"));
            Assert.That(component.FindAll(".ec-text-editor-toolbar button[aria-label='Zpět']"), Has.Count.EqualTo(1));
        }

        editors.Text = "x\n";
        await component.InvokeAsync(async () => await component.FindComponent<TextEditor>().Instance.NotifyChanged(dirty: true, canUndo: true, canRedo: false));
        await ModeButton(component, "Formátovaně").ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(markdown.Opened[^1], Is.EqualTo(("x\n", true, Label)), "the source change goes across to the formatted editor"));

        Assert.That(SaveButton(component).HasAttribute("disabled"), Is.False, "the change stays unsaved");
    }

    [Test]
    public async Task TheLinkWindowLinksASafeAddress()
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await Changed(component, dirty: false, [], []);
        await ToolbarButton(component, "Odkaz").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(LinkWindow), Has.Count.EqualTo(1), "Odkaz opens the link window");
            Assert.That(LinkButtons(component), Does.Not.Contain("Odebrat odkaz"), "there is no link to remove");
        }

        await component.Find("#markdown-link-href").InputAsync(new ChangeEventArgs { Value = "https://example.cz" });
        await component.Find("#markdown-link-href").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(markdown.Links, Is.EqualTo(["https://example.cz"]), "Enter links the address");
            Assert.That(component.FindAll(LinkWindow), Is.Empty);
        }
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
    public async Task TheLinkWindowRemovesAnExistingLink()
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        markdown.Link = "https://a.cz";

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await ToolbarButton(component, "Odkaz").ClickAsync(new MouseEventArgs());

        Assert.That(component.Find("#markdown-link-href").GetAttribute("value"), Is.EqualTo("https://a.cz"), "the window shows the link at the cursor");

        await LinkButton(component, "Odebrat odkaz").ClickAsync(new MouseEventArgs());

        Assert.That(markdown.Links, Is.EqualTo([""]), "Odebrat odkaz removes the link");
    }

    [Test]
    public async Task AFormattedEditThatDoesNotStartSaysSo()
    {
        await using var ctx = NewContext(out var markdown, out _, out _);

        markdown.EditFails = true;

        var file = File("Poznámky.md");
        var component = await RenderEditing(ctx, [file], file.FileId);

        await component.WaitForAssertionAsync(() => Assert.That(markdown.Shown, Is.EqualTo([Original]), "the text as read shows again"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-alert").TextContent, Is.EqualTo("Úpravy se nepodařilo spustit. Zkuste to za chvíli znovu."));
            Assert.That(HeaderButtons(component), Does.Not.Contain("Uložit"), "the edit ended");
        }
    }

    [Test]
    public async Task AMarkdownWithADetachedSignatureWarnsWhileEditing()
    {
        await using var ctx = NewContext(out _, out _, out _);

        var signature = File("Smlouva.md.p7s");
        var file = File("Smlouva.md") with { SignatureFileId = signature.FileId };
        var component = await RenderEditing(ctx, [file, signature], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-text-editor-note").TextContent, Does.Contain("Nová verze ho zneplatní."), "a new version invalidates the detached signature");
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty, "the signers do not show while editing");
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

    private static List<string?> ToolbarLabels(IRenderedComponent<FilePreviewModal> component, string selector)
    {
        return [.. component.FindAll($"{selector} button").Select(static button => button.GetAttribute("aria-label"))];
    }

    private static AngleSharp.Dom.IElement ToolbarButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-markdown-toolbar button[aria-label='{label}']");
    }

    private static AngleSharp.Dom.IElement ModeButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-markdown-modes button[aria-label='{label}']");
    }

    private static List<string> LinkButtons(IRenderedComponent<FilePreviewModal> component)
    {
        return [.. component.FindAll($"{LinkWindow} .ec-modal-footer button").Select(static button => button.TextContent.Trim())];
    }

    private static AngleSharp.Dom.IElement LinkButton(IRenderedComponent<FilePreviewModal> component, string text)
    {
        return component
            .FindAll($"{LinkWindow} .ec-modal-footer button")
            .Single(button => string.Equals(button.TextContent.Trim(), text, StringComparison.Ordinal));
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

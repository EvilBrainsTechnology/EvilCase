using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilesCardRenderTests
{
    [Test]
    public void FilesHandedToTheHiddenInputUploadAndReloadTheList()
    {
        using var ctx = NewContext();

        var logger = new CapturingLogger<FilesCard>();
        var uploaded = new List<string>();
        var loads = 0;

        ctx.Services.AddSingleton<ILogger<FilesCard>>(logger);

        var component = new Card
        {
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([]);
            },
            UploadFile = (file, _) =>
            {
                uploaded.Add(file.Name);

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        component.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "a.txt"),
            InputFileContent.CreateFromText("b", "b.txt"));

        // The reload is the last step of the batch, so waiting for it waits for the uploads too.
        component.WaitForAssertion(() => Assert.That(loads, Is.EqualTo(2), "the list reloads once the batch finishes"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.EqualTo(["a.txt", "b.txt"]), "a card-wide drop uploads every dropped file, in order");
            Assert.That(logger.LoggedWarningOrAbove, Is.False, "an upload that succeeds logs nothing");
        }
    }

    [Test]
    public async Task ADropCarryingNoFileUploadsNothingAndShowsNoError()
    {
        await using var ctx = NewContext();

        var uploaded = new List<string>();
        var loads = 0;

        var component = new Card
        {
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([]);
            },
            UploadFile = (file, _) =>
            {
                uploaded.Add(file.Name);

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        // Dropping dragged text or a link hands the input an empty file list, which the drop zone
        // reports like any other change. UploadFiles refuses to raise an empty one, so the input's
        // own OnChange is raised instead.
        var input = component.FindComponent<InputFile>();
        var change = new InputFileChangeEventArgs([]);

        await component.InvokeAsync(async () => await input.Instance.OnChange.InvokeAsync(change));

        await component.WaitForAssertionAsync(() => Assert.That(loads, Is.EqualTo(2), "the list reloads once the empty batch finishes"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.Empty, "a drop carrying no file starts no upload");
            Assert.That(component.FindAll(".ec-alert"), Is.Empty, "a drop carrying no file reports no failure");
        }
    }

    [Test]
    public void AFinishedBatchLeavesAFreshInputBehind()
    {
        using var ctx = NewContext();

        var component = new Card().Render(ctx);

        var before = component.FindComponent<InputFile>().Instance;

        component.FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("a", "a.txt"));

        component.WaitForAssertion(() => Assert.That(
            component.FindComponent<InputFile>().Instance,
            Is.Not.SameAs(before),
            "a browser raises no change event for a file already in the input, so picking the same file again needs a fresh one"));
    }

    [Test]
    public void TheHiddenFileInputSitsInsideTheDropZone()
    {
        using var ctx = NewContext();

        var component = new Card().Render(ctx);

        // The drop zone hands a drop to the first file input inside its own div and nowhere else.
        var dropZone = component.Find("div.ec-file-card");

        Assert.That(dropZone.QuerySelector("input[type=file]"), Is.Not.Null, "the drop target holds the input a drop is handed to");
    }

    [Test]
    public void TheUploadButtonOpensTheInputADropGoesTo()
    {
        using var ctx = NewContext();

        var component = new Card().Render(ctx);

        var upload = component.Find("label.ec-file-upload");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(upload.GetAttribute("for"), Is.EqualTo(component.Find("input[type=file]").Id), "a picked and a dropped file take one input");
            Assert.That(upload.ClassList, Does.Contain("ec-button-secondary").And.Not.Contain("ec-button-primary"), "the upload button looks like every other secondary button");
        }
    }

    [Test]
    public void TheCardNamesWhatItBelongsTo()
    {
        using var ctx = NewContext();

        var component = new Card { Title = "Soubory úkonu" }.Render(ctx);

        Assert.That(component.Find(".ec-card-title").TextContent.Trim(), Is.EqualTo("Soubory úkonu"));
    }

    [Test]
    public void AnEmptyListShowsTheEmptyStateAndNoFileRow()
    {
        using var ctx = NewContext();

        var component = new Card().Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-empty-text").TextContent, Is.EqualTo("Zatím tu nejsou žádné soubory."));
            Assert.That(component.Find(".ec-empty-hint").TextContent, Is.EqualTo("Přetáhněte je kamkoli do panelu nebo použijte Nahrát soubory."));
            Assert.That(component.FindAll(".ec-file-row"), Is.Empty);
        }
    }

    [Test]
    public void TheHeaderInvitesADropAndNoStripSitsBelowTheList()
    {
        using var ctx = NewContext();

        var component = new Card { Files = [PdfFile()] }.Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-card-header .ec-file-drop-hint").TextContent.Trim(), Is.EqualTo("Přetáhněte soubory sem nebo"));
            Assert.That(component.Find(".ec-card-header label.ec-file-upload").TextContent.Trim(), Is.EqualTo("Nahrát soubory"));
            Assert.That(component.Find("label.ec-file-upload .ec-file-upload-more").TextContent, Is.EqualTo(" soubory"), "the mobile button drops the word it wraps");
            Assert.That(component.FindAll(".ec-file-drop"), Is.Empty, "the drop strip below the list is gone");
            Assert.That(component.FindAll(".ec-file-drop-compact"), Is.Empty);
        }
    }

    [Test]
    public void TheCompactHeaderKeepsTheDropHintShort()
    {
        using var ctx = NewContext();

        var component = new Card { Files = [PdfFile()], Compact = true }.Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-file-drop-hint").TextContent.Trim(), Is.EqualTo("Přetáhněte sem nebo"));
            Assert.That(component.Find("label.ec-file-upload").TextContent.Trim(), Is.EqualTo("Nahrát"));
        }
    }

    [Test]
    public void TheDropOverlayNamesTheOwnerAndTheLimits()
    {
        using var ctx = NewContext();

        var component = new Card { UploadTarget = "k úkonu" }.Render(ctx);

        var overlay = component.Find(".ec-file-card .ec-file-drop-overlay");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(overlay.QuerySelector(".ec-file-drop-title")!.TextContent, Is.EqualTo("Pusťte soubory a nahrají se k úkonu"));
            Assert.That(overlay.QuerySelector(".ec-file-drop-limits")!.TextContent, Is.EqualTo("Nejvýše 100 souborů, každý do 100 MB"));
        }
    }

    [Test]
    public void AFileRowShowsAnIconTileForItsTypeAndNoExtensionText()
    {
        using var ctx = NewContext();

        (string FileName, string Tone)[] cases =
        [
            ("a.pdf", "pdf"),
            ("a.docx", "document"),
            ("a.xlsx", "sheet"),
            ("a.pptx", "presentation"),
            ("a.txt", "text"),
            ("a.png", "image"),
            ("a.mp3", "audio"),
            ("a.mp4", "video"),
            ("a.eml", "email"),
            ("a.zfo", "message"),
            ("a.p7s", "signature"),
            ("a.json", "code"),
            ("a.zip", "archive"),
            ("a.bin", "other"),
            ("README", "other"),
        ];

        var component = new Card { Files = [.. cases.Select(static item => File(item.FileName))] }.Render(ctx);

        var tiles = component.FindAll(".ec-file-row .ec-file-icon");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(tiles, Has.Count.EqualTo(cases.Length));

            for (var index = 0; index < cases.Length; index++)
            {
                Assert.That(tiles[index].ClassList, Does.Contain($"ec-file-icon-{cases[index].Tone}"), cases[index].FileName);
                Assert.That(tiles[index].TextContent.Trim(), Is.Empty, "a tile carries a glyph and no extension text");
                Assert.That(tiles[index].QuerySelector("svg path"), Is.Not.Null);
            }
        }
    }

    [Test]
    public void TheWholeRowOpensThePreviewOfAPreviewableFile()
    {
        using var ctx = NewContext();

        var blobs = new StubBlobUrls();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());

        var downloads = 0;
        var component = new Card
        {
            Files = [PdfFile()],
            DownloadFile = (_, _) =>
            {
                downloads++;

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        var open = component.Find(".ec-file-row button.ec-file-name");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(open.GetAttribute("title"), Is.EqualTo("Otevřít náhled"));
            Assert.That(component.FindAll("dialog"), Is.Empty);
            Assert.That(component.Find(".ec-file-actions").Closest(".ec-file-name"), Is.Null, "the row actions sit outside the control that opens the row");
        }

        open.Click();

        component.WaitForAssertion(() => Assert.That(component.FindAll("dialog iframe.ec-preview-frame"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloads, Is.Zero, "a previewable file opens in the preview and is not downloaded");
            Assert.That(component.Find("dialog .ec-preview-name").TextContent, Is.EqualTo("Rozhodnutí o přestupku.pdf"));
            Assert.That(blobs.Created, Is.EqualTo([("application/pdf", "blob:test/0")]));
            Assert.That(component.Find("iframe.ec-preview-frame").GetAttribute("src"), Is.EqualTo("blob:test/0"));
        }
    }

    [Test]
    public void TheRowOfAFileWithoutPreviewDownloadsIt()
    {
        using var ctx = NewContext();

        var downloaded = new List<string>();
        var component = new Card
        {
            Files = [File("Doručenka.zfo")],
            DownloadFile = (file, _) =>
            {
                downloaded.Add(file.FileName);

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        var open = component.Find(".ec-file-row button.ec-file-name");

        open.Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(open.GetAttribute("title"), Is.EqualTo("Stáhnout"));
            Assert.That(downloaded, Is.EqualTo(["Doručenka.zfo"]));
            Assert.That(component.FindAll("dialog"), Is.Empty, "a file without preview opens no preview");
        }
    }

    [Test]
    public void TheDownloadButtonDownloadsWithoutOpeningThePreview()
    {
        using var ctx = NewContext();

        var downloads = 0;
        var component = new Card
        {
            Files = [PdfFile()],
            DownloadFile = (_, _) =>
            {
                downloads++;

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        var download = component.Find("button[aria-label='Stáhnout Rozhodnutí o přestupku.pdf']");

        download.Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloads, Is.EqualTo(1));
            Assert.That(download.ClassList, Does.Contain("ec-button-secondary").And.Contain("ec-button-icon"));
            Assert.That(download.TextContent.Trim(), Is.Empty);
            Assert.That(component.FindAll("dialog"), Is.Empty);
        }
    }

    [Test]
    public void AFileRowShowsItsSizeAndWhenItWasUploaded()
    {
        using var ctx = NewContext();

        var file = PdfFile();
        var component = new Card { Files = [file] }.Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-file-meta").TextContent, Is.EqualTo($"1 kB · {MomentDisplay.Text(file.Created)}"));
            Assert.That(component.Find(".ec-file-meta .ec-file-moment").TextContent, Is.EqualTo(MomentDisplay.Text(file.Created)), "the moment never breaks inside");
        }
    }

    [Test]
    public void TheCompactRowKeepsTheNameOnOneLineAndOffersIconButtons()
    {
        using var ctx = NewContext();

        var file = PdfFile();
        var component = new Card { Files = [file], Compact = true }.Render(ctx);

        var download = component.Find("button[aria-label='Stáhnout Rozhodnutí o přestupku.pdf']");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("div.ec-file-card-compact"), Has.Count.EqualTo(1));
            Assert.That(component.Find("button.ec-file-name").GetAttribute("title"), Is.EqualTo("Rozhodnutí o přestupku.pdf"));
            Assert.That(component.Find(".ec-file-meta").TextContent, Is.EqualTo($"1 kB · {MomentDisplay.Text(file.Created)}"));
            Assert.That(download.ClassList, Does.Contain("ec-button-icon"));
            Assert.That(download.TextContent.Trim(), Is.Empty);
            Assert.That(component.Find(".ec-file-icon").ClassList, Does.Contain("ec-file-icon-compact"));
        }
    }

    [Test]
    public void AFailedListShowsTheErrorAndRetryingReloads()
    {
        using var ctx = NewContext();

        var calls = 0;

        var component = new Card
        {
            LoadFiles = _ =>
            {
                calls++;

                if (calls == 1)
                    throw new HttpRequestException();

                return Task.FromResult<IReadOnlyList<FileListItem>>([PdfFile()]);
            },
        }
            .Render(ctx);

        Assert.That(
            component.Find(".ec-card-error .ec-alert").TextContent,
            Does.Contain("Soubory se nepodařilo načíst"));

        component.Find(".ec-card-error button").Click();

        component.WaitForAssertion(() => Assert.That(
            component.FindAll(".ec-file-row"),
            Has.Count.EqualTo(1),
            "the retry reloads the list"));
    }

    [Test]
    public async Task TheMenuOffersRenameAndADangerDeleteAndNoDeleteButtonSitsInTheRow()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [PdfFile()] }.Render(ctx);

        Assert.That(component.FindAll(".ec-file-actions button"), Has.Count.EqualTo(2), "a row carries Stáhnout and the menu and nothing else");

        await OpenMenu(component);

        var items = component.FindAll("[role=menuitem]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Select(static item => item.TextContent.Trim()), Is.EqualTo(["Přejmenovat", "Smazat…"]));
            Assert.That(items[1].ClassList, Does.Contain("ec-menu-item-danger"));
        }
    }

    [Test]
    public async Task DeletingAFileFromTheMenuOpensTheConfirmationNamingIt()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [File("a.pdf")] }.Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-modal-footer"), Is.Not.Empty);
            Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain("a.pdf"));
        }
    }

    [Test]
    public async Task RenamingTurnsTheRowIntoAnInputWithTheExtensionFixed()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [PdfFile()] }.Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-file-row input.ec-file-rename-input").GetAttribute("value"), Is.EqualTo("Rozhodnutí o přestupku"));
            Assert.That(component.Find(".ec-file-rename-ext").TextContent, Is.EqualTo(".pdf"));
            Assert.That(component.FindAll(".ec-file-rename .ec-button").Select(static button => button.TextContent.Trim()), Is.EqualTo(["Uložit", "Zrušit"]));
            Assert.That(component.FindAll("button.ec-file-name"), Is.Empty, "a row being renamed does not open the preview");
        }
    }

    [Test]
    public async Task SavingARenameSendsTheNameWithTheExtensionAndReloadsTheList()
    {
        await using var ctx = NewContext();

        var renamed = new List<(Guid FileId, string FileName)>();
        var loads = 0;
        var file = PdfFile();

        var component = new Card
        {
            Files = [file],
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([file]);
            },
            RenameFile = (item, name, _) =>
            {
                renamed.Add((item.FileId, name));

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Nový název" });
        await component.FindAll(".ec-file-rename .ec-button")[0].ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-rename"), Is.Empty, "a saved rename leaves the input"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renamed, Is.EqualTo([(file.FileId, "Nový název.pdf")]));
            Assert.That(loads, Is.EqualTo(2), "the list reloads after a rename");
        }
    }

    [Test]
    public async Task EnterSavesAndEscapeCancelsTheRename()
    {
        await using var ctx = NewContext();

        var renamed = new List<string>();
        var component = new Card
        {
            Files = [PdfFile()],
            RenameFile = (_, name, _) =>
            {
                renamed.Add(name);

                return Task.CompletedTask;
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Zrušený" });
        await component.Find("input.ec-file-rename-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(component.FindAll(".ec-file-rename"), Is.Empty, "Escape leaves the input");

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Uložený" });
        await component.Find("input.ec-file-rename-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-rename"), Is.Empty));

        Assert.That(renamed, Is.EqualTo(["Uložený.pdf"]), "only Enter saved");
    }

    [TestCase("Escape")]
    [TestCase("Enter")]
    public async Task AFinishedRenameReturnsTheFocusToTheRow(string key)
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [PdfFile()] }.Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Nový" });
        await component.Find("input.ec-file-rename-input").KeyDownAsync(new KeyboardEventArgs { Key = key });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-rename"), Is.Empty));

        var buttonId = component.Find("button.ec-file-name").GetAttribute("blazor:elementReference");
        var focused = ctx.JSInterop.Invocations["Blazor._internal.domWrapper.focus"];

        Assert.That(((ElementReference)focused[^1].Arguments[0]!).Id, Is.EqualTo(buttonId), "keyboard focus never drops to the page when the input goes away");
    }

    [Test]
    public async Task ABlankNameIsRefusedBeforeTheCallAndAFailureShowsInTheRow()
    {
        await using var ctx = NewContext();

        var calls = 0;
        var component = new Card
        {
            Files = [PdfFile()],
            RenameFile = (_, _, _) =>
            {
                calls++;

                throw new ApiException(HttpStatusCode.NotFound, responseBody: null);
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "   " });
        await component.FindAll(".ec-file-rename .ec-button")[0].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-file-rename .ec-alert").TextContent, Does.Contain("Zadejte název souboru"));
            Assert.That(calls, Is.Zero, "a blank name never reaches the API");
        }

        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Nový" });
        await component.FindAll(".ec-file-rename .ec-button")[0].ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-file-rename .ec-alert").TextContent, Does.Contain("Soubor už neexistuje")));
        Assert.That(component.FindAll(".ec-file-rename"), Has.Count.EqualTo(1), "a failed rename keeps the input");
    }

    [Test]
    public void ThePreviewStepsThroughEveryFileOfTheCard()
    {
        using var ctx = NewContext();

        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());

        var component = new Card { Files = [PdfFile(), File("Doručenka.zfo"), File("Foto.jpg")] }.Render(ctx);

        component.Find(".ec-file-row button.ec-file-name").Click();
        component.WaitForAssertion(() => Assert.That(component.FindAll("dialog iframe"), Has.Count.EqualTo(1)));

        Assert.That(component.Find(".ec-preview-position").TextContent, Is.EqualTo("1 z 3"));

        component.Find("button[aria-label='Další soubor']").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-position").TextContent, Is.EqualTo("2 z 3"));
            Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Doručenka.zfo"));
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"));
        }
    }

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();

        // FileDropZone imports its own module from a path carrying a version query string, which
        // no SetupModule can name, and reads a property bUnit's runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        return ctx;
    }

    private static async Task OpenMenu(IRenderedComponent<FilesCard> component)
    {
        await component.Find(".ec-file-actions button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
    }

    private static FileListItem PdfFile()
    {
        return File("Rozhodnutí o přestupku.pdf");
    }

    private static FileListItem File(string fileName)
    {
        return new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = fileName,
            SizeBytes = 1024,
            Created = new DateTime(2025, 9, 2, 10, 0, 0, DateTimeKind.Utc),
        };
    }

    private sealed class Card
    {
        public string Title { get; init; } = "Soubory spisu";

        public string UploadTarget { get; init; } = "ke spisu";

        public bool Compact { get; init; }

        public IReadOnlyList<FileListItem> Files { get; init; } = [];

        public Func<CancellationToken, Task<IReadOnlyList<FileListItem>>>? LoadFiles { get; init; }

        public Func<IBrowserFile, CancellationToken, Task> UploadFile { get; init; } = static (_, _) => Task.CompletedTask;

        public Func<FileListItem, CancellationToken, Task> DownloadFile { get; init; } = static (_, _) => Task.CompletedTask;

        public Func<FileListItem, string, CancellationToken, Task> RenameFile { get; init; } = static (_, _, _) => Task.CompletedTask;

        public IRenderedComponent<FilesCard> Render(BunitContext ctx)
        {
            var files = this.Files;
            var loadFiles = this.LoadFiles ?? (_ => Task.FromResult(files));

            return ctx.Render<FilesCard>(parameters => parameters
                .Add(static card => card.Title, this.Title)
                .Add(static card => card.LoadFiles, loadFiles)
                .Add(static card => card.UploadFile, this.UploadFile)
                .Add(static card => card.DownloadFile, this.DownloadFile)
                .Add(static card => card.LoadContent, static (_, _) => Task.FromResult<Stream>(new MemoryStream("abc"u8.ToArray())))
                .Add(static card => card.RenameFile, this.RenameFile)
                .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
                .Add(static card => card.OwnerGoneError, "spis už neexistuje.")
                .Add(static card => card.UploadTarget, this.UploadTarget)
                .Add(static card => card.Compact, this.Compact));
        }
    }
}

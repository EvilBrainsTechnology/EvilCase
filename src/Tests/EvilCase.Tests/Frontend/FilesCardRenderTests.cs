using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
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

                return Task.FromResult(Stored(file.Name));
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
    public async Task ADropDuringAnUploadGoesToAFreshInputAndIsRejected()
    {
        await using var ctx = NewContext();

        var gate = new TaskCompletionSource();
        var uploaded = new List<string>();
        var loads = 0;

        var component = new Card
        {
            LoadFiles = _ =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileListItem>>([]);
            },
            UploadFile = async (file, _) =>
            {
                uploaded.Add(file.Name);
                await (string.Equals(file.Name, "a.txt", StringComparison.Ordinal) ? gate.Task : Task.CompletedTask);

                return Stored(file.Name);
            },
        }
            .Render(ctx);

        var first = component.FindComponent<InputFile>().Instance;
        var firstBatch = component.InvokeAsync(async () => await first.OnChange.InvokeAsync(Change("a.txt", "b.txt")));

        // InvokeAsync queues behind work the renderer has not finished, so the change can run after it returns.
        await component.WaitForAssertionAsync(() => Assert.That(uploaded, Is.EqualTo(["a.txt"])));

        var inputs = component.FindComponents<InputFile>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(inputs, Has.Count.EqualTo(3));
            Assert.That(inputs[1].Instance, Is.SameAs(first), "a change empties the input's file list, so a batch keeps its own input, beside the version input");
            Assert.That(component.Find("label.ec-file-upload").GetAttribute("for"), Is.EqualTo(component.FindAll("input[type=file]")[0].Id), "the next pick goes to the fresh input");
        }

        await Drop(component);

        Assert.That(component.FindAll(".ec-alert"), Is.Empty, "dragged text or a paste carries no file and asks nothing of the user");

        await Drop(component, "c.txt");
        await Drop(component, "d.txt");

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-alert").Select(static alert => alert.TextContent), Is.EqualTo(["Počkejte, až se dokončí nahrávání."]), "a repeated drop asks to wait once"));

        gate.SetResult();
        await firstBatch;

        await component.WaitForAssertionAsync(() => Assert.That(loads, Is.EqualTo(2), "the list reloads once the batch finishes"));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.EqualTo(["a.txt", "b.txt"]), "a drop during an upload uploads nothing and leaves the batch in flight whole");
            Assert.That(component.FindComponents<InputFile>(), Has.Count.EqualTo(2), "a finished batch takes its input with it, beside the version input");
            Assert.That(component.FindAll(".ec-file-uploading"), Is.Empty);
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

                return Task.FromResult(Stored(file.Name));
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
    public void TheCompactHeaderCarriesOnlyTheTitleAndTheUploadButton()
    {
        using var ctx = NewContext();

        var component = new Card { Compact = true }.Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-card-count"), Is.Empty);
            Assert.That(component.FindAll(".ec-file-drop-hint"), Is.Empty);
            Assert.That(component.Find("label.ec-file-upload").TextContent.Trim(), Is.EqualTo("Nahrát"));
            Assert.That(component.Find(".ec-empty-text").TextContent, Is.EqualTo("Zatím tu nejsou žádné soubory."));
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

        var downloader = ctx.Services.GetRequiredService<StubFileDownloader>();
        var file = PdfFile();
        var component = new Card { Files = [file] }.Render(ctx);

        var open = component.Find(".ec-file-row button.ec-file-name");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(open.GetAttribute("title"), Is.EqualTo("Otevřít náhled"));
            Assert.That(component.FindAll("dialog"), Is.Empty);
            Assert.That(component.Find(".ec-file-actions").Closest(".ec-file-name"), Is.Null, "the row actions sit outside the control that opens the row");
        }

        open.Click();

        component.WaitForAssertion(() => Assert.That(component.FindAll("dialog .ec-preview-pdf"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloader.Downloaded, Is.Empty, "a previewable file opens in the preview and is not downloaded");
            Assert.That(component.Find("dialog .ec-preview-name").TextContent, Is.EqualTo("Rozhodnutí o přestupku.pdf"));
            Assert.That(component.FindAll("iframe"), Is.Empty);
        }
    }

    [Test]
    public void TheRowOfAFileWithoutPreviewOpensTheModalThatSaysSo()
    {
        using var ctx = NewContext();

        var downloader = ctx.Services.GetRequiredService<StubFileDownloader>();
        var component = new Card { Files = [File("Smlouva.doc")] }.Render(ctx);

        var open = component.Find(".ec-file-row button.ec-file-name");

        open.Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(open.GetAttribute("title"), Is.EqualTo("Otevřít náhled"));
            Assert.That(component.Find("dialog .ec-preview-name").TextContent, Is.EqualTo("Smlouva.doc"));
            Assert.That(component.Find("dialog .ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"));
            Assert.That(downloader.Downloaded, Is.Empty, "opening a file never downloads it");
        }
    }

    [Test]
    public void TheDownloadButtonDownloadsWithoutOpeningThePreview()
    {
        using var ctx = NewContext();

        var downloader = ctx.Services.GetRequiredService<StubFileDownloader>();
        var file = PdfFile();
        var component = new Card { Files = [file] }.Render(ctx);

        var download = component.Find("button[aria-label='Stáhnout Rozhodnutí o přestupku.pdf']");

        download.Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloader.Downloaded.Single(), Is.EqualTo((file.FileId, (int?)null, file.FileName)));
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
    public async Task TheMenuOffersANewVersionTheVersionsRenameAndADangerDelete()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [PdfFile()] }.Render(ctx);

        Assert.That(component.FindAll(".ec-file-actions button"), Has.Count.EqualTo(2), "a row carries Stáhnout and the menu and nothing else");

        await OpenMenu(component);

        var items = component.FindAll("[role=menuitem]");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Select(static item => item.TextContent.Trim()), Is.EqualTo(["Nahrát novou verzi", "Verze (1)", "Přejmenovat", "Smazat"]));
            Assert.That(items[3].ClassList, Does.Contain("ec-menu-item-danger"));
        }
    }

    [Test]
    public async Task DeletingAFileFromTheMenuOpensTheConfirmationNamingIt()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [File("a.pdf")] }.Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[3].ClickAsync(new MouseEventArgs());

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
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());

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
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
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
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Zrušený" });
        await component.Find("input.ec-file-rename-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        Assert.That(component.FindAll(".ec-file-rename"), Is.Empty, "Escape leaves the input");

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
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
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
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
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
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
    public async Task ARenameOntoATakenNameSaysSo()
    {
        await using var ctx = NewContext();

        var component = new Card
        {
            Files = [PdfFile()],
            RenameFile = static (_, _, _) => throw new ApiException(HttpStatusCode.Conflict, responseBody: null),
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[2].ClickAsync(new MouseEventArgs());
        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Obsazený" });
        await component.FindAll(".ec-file-rename .ec-button")[0].ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-file-rename .ec-alert").TextContent, Does.Contain("Soubor tohoto jména už existuje")));
        Assert.That(component.FindAll(".ec-file-rename-input"), Has.Count.EqualTo(1), "a refused rename keeps the input");
    }

    [Test]
    public void ThePreviewStepsThroughEveryFileOfTheCard()
    {
        using var ctx = NewContext();

        var component = new Card { Files = [PdfFile(), File("Smlouva.doc"), File("Foto.jpg")] }.Render(ctx);

        component.Find(".ec-file-row button.ec-file-name").Click();
        component.WaitForAssertion(() => Assert.That(component.FindAll("dialog .ec-preview-pdf"), Has.Count.EqualTo(1)));

        Assert.That(component.Find(".ec-preview-position").TextContent, Is.EqualTo("Soubor 1 z 3"));

        component.Find("button[aria-label='Další soubor']").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-position").TextContent, Is.EqualTo("Soubor 2 z 3"));
            Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Smlouva.doc"));
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"));
        }
    }

    private static FileUploadResult Stored(FileListItem file, FileUploadOutcome outcome)
    {
        return new FileUploadResult { File = file with { Version = file.Version + 1, VersionCount = file.VersionCount + 1 }, Outcome = outcome };
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

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static FileUploadResult Stored(string name)
    {
        return new FileUploadResult
        {
            File = new FileListItem { FileId = Guid.CreateVersion7(), FileName = name, SizeBytes = 1, Created = DateTime.UtcNow, Version = 1, VersionCount = 1 },
            Outcome = FileUploadOutcome.NewFile,
        };
    }

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();

        // FileDropZone imports its own module from a path carrying a version query string, which
        // no SetupModule can name, and reads a property bUnit's runtime does not answer.
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IJSRuntime>(new PropertyReadingJSRuntime(ctx.JSInterop.JSRuntime));

        var downloader = new StubFileDownloader();
        ctx.Services.AddSingleton(downloader);
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubFilePicker());
        ctx.Services.AddSingleton<IFilePicker>(static sp => sp.GetRequiredService<StubFilePicker>());
        ctx.Services.AddSingleton<IBlobUrls>(new StubBlobUrls());
        ctx.Services.AddSingleton<IFilePreviews>(new StubFilePreviews());
        ctx.Services.AddSingleton<IAuthSession>(new StubAuthSession());

        return ctx;
    }

    [Test]
    public void AMultiVersionRowCarriesItsVersionChipAndASingleVersionRowNone()
    {
        using var ctx = NewContext();

        var component = new Card { Files = [PdfFile(), Versioned("a.txt", 3, 3)] }.Render(ctx);

        var rows = component.FindAll(".ec-file-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(rows[0].QuerySelector(".ec-file-meta .ec-badge"), Is.Null);
            Assert.That(rows[1].QuerySelector(".ec-file-meta .ec-badge-file-version")!.TextContent.Trim(), Is.EqualTo("verze 3"));
        }
    }

    [Test]
    public void TheCompactRowNamesTheVersionInItsMetaLine()
    {
        using var ctx = NewContext();

        var file = Versioned("a.txt", 2, 2);
        var component = new Card { Files = [file], Compact = true }.Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Normalize(component.Find(".ec-file-meta").TextContent), Is.EqualTo($"verze 2 · 1 kB · {MomentDisplay.Text(file.Created)}"));
            Assert.That(component.FindAll(".ec-file-meta .ec-badge"), Is.Empty);
        }
    }

    [Test]
    public async Task ABatchReportsNewVersionsAndUnchangedFilesAndHighlightsWhatItStored()
    {
        await using var ctx = NewContext();

        var stored = File("nový.txt");
        var same = Versioned("stejný.txt", 2, 2);
        var added = Versioned("další.txt", 3, 3);
        var component = new Card
        {
            Files = [stored, same, added],
            UploadFile = (picked, _) => Task.FromResult(picked.Name switch
            {
                "nový.txt" => new FileUploadResult { File = stored, Outcome = FileUploadOutcome.NewFile },
                "stejný.txt" => new FileUploadResult { File = same, Outcome = FileUploadOutcome.Unchanged },
                _ => new FileUploadResult { File = added, Outcome = FileUploadOutcome.NewVersion },
            }),
        }
            .Render(ctx);

        component.FindComponent<InputFile>().UploadFiles(
            InputFileContent.CreateFromText("a", "nový.txt"),
            InputFileContent.CreateFromText("b", "stejný.txt"),
            InputFileContent.CreateFromText("c", "další.txt"));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-notice span"), Has.Count.EqualTo(2)));

        var rows = component.FindAll(".ec-file-row");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-file-notice span").Select(static notice => notice.TextContent),
                Is.EqualTo(["stejný.txt: obsah je stejný jako poslední verze, nová verze nevznikla.", "další.txt: nahráno jako verze 3."]));
            Assert.That(rows[0].ClassList, Does.Contain("ec-file-row-fresh"));
            Assert.That(rows[0].QuerySelector(".ec-badge-fresh")!.TextContent.Trim(), Is.EqualTo("nový"));
            Assert.That(rows[1].ClassList, Does.Not.Contain("ec-file-row-fresh"), "an unchanged file marks nothing");
            Assert.That(rows[1].QuerySelector(".ec-badge-fresh"), Is.Null);
            Assert.That(rows[2].ClassList, Does.Contain("ec-file-row-fresh"));
            Assert.That(rows[2].QuerySelector(".ec-badge-fresh")!.TextContent.Trim(), Is.EqualTo("nová verze"));
        }
    }

    [Test]
    public async Task ANewVersionFromTheMenuOpensThePickerAndUploadsTheFile()
    {
        await using var ctx = NewContext();

        var picker = ctx.Services.GetRequiredService<StubFilePicker>();
        var uploaded = new List<(string File, string Picked)>();
        var file = File("a.pdf");
        var component = new Card
        {
            Files = [file],
            UploadFileVersion = (target, picked, _) =>
            {
                uploaded.Add((target.FileName, picked.Name));

                return Task.FromResult(Stored(target, FileUploadOutcome.NewVersion));
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        Assert.That(picker.Picked, Has.Count.EqualTo(1));

        VersionInput(component).UploadFiles(InputFileContent.CreateFromText("x", "nová.pdf"));

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-file-notice").TextContent.Trim(), Is.EqualTo("a.pdf: nahráno jako verze 2.")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.EqualTo([("a.pdf", "nová.pdf")]));
            Assert.That(component.Find(".ec-file-row .ec-badge-fresh").TextContent.Trim(), Is.EqualTo("nová verze"));
        }
    }

    [Test]
    public async Task ANewVersionWithAnotherExtensionIsRefusedWithoutAnUpload()
    {
        await using var ctx = NewContext();

        var calls = 0;
        var component = new Card
        {
            Files = [File("a.pdf")],
            UploadFileVersion = (target, _, _) =>
            {
                calls++;

                return Task.FromResult(Stored(target, FileUploadOutcome.NewVersion));
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[0].ClickAsync(new MouseEventArgs());

        VersionInput(component).UploadFiles(InputFileContent.CreateFromText("x", "návrh.docx"));

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-alert").TextContent, Is.EqualTo("návrh.docx: nová verze souboru a.pdf musí mít příponu .pdf.")));

        Assert.That(calls, Is.Zero);
    }

    [Test]
    public async Task TheVersionsItemOpensTheVersionsOfTheFile()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [Versioned("a.pdf", 3, 3)], LoadFileVersions = static (_, _) => Task.FromResult(VersionItems()) }.Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Normalize(component.Find(".ec-versions-subtitle").TextContent), Is.EqualTo("a.pdf · 3 verze"));
            Assert.That(component.FindAll(".ec-versions-row"), Has.Count.EqualTo(3));
        }
    }

    [Test]
    public async Task DeletingAMultiVersionFileSaysHowManyVersionsGo()
    {
        await using var ctx = NewContext();

        var component = new Card { Files = [Versioned("a.pdf", 3, 3)] }.Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[3].ClickAsync(new MouseEventArgs());

        Assert.That(component.Find(".ec-confirm-text").TextContent, Does.Contain("se všemi 3 verzemi"));
    }

    [Test]
    public async Task AVersionPreviewedFromTheVersionsOpensThatVersion()
    {
        await using var ctx = NewContext();

        var loaded = new List<int?>();
        var component = new Card
        {
            Files = [Versioned("a.txt", 3, 3)],
            LoadFileVersions = static (_, _) => Task.FromResult(VersionItems()),
            LoadContent = (_, version, _) =>
            {
                loaded.Add(version);

                return Task.FromResult<Stream>(new MemoryStream("abc"u8.ToArray()));
            },
        }
            .Render(ctx);

        await OpenMenu(component);
        await component.FindAll("[role=menuitem]")[1].ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-versions-row")[1].QuerySelector(".ec-versions-preview button")!.ClickAsync(new MouseEventArgs());

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1)));

        Assert.That(loaded, Is.EqualTo(new int?[] { 2 }));
    }

    private static IRenderedComponent<InputFile> VersionInput(IRenderedComponent<FilesCard> component)
    {
        return component.FindComponents<InputFile>().Single(static input => input.Instance.AdditionalAttributes is { } attributes && attributes.TryGetValue("id", out var id) && Equals(id, "files-version-input"));
    }

    private static async Task OpenMenu(IRenderedComponent<FilesCard> component)
    {
        await component.Find(".ec-file-actions button[aria-haspopup=menu]").ClickAsync(new MouseEventArgs());
    }

    private static InputFileChangeEventArgs Change(params string[] fileNames)
    {
        return new InputFileChangeEventArgs([.. fileNames.Select(static name => new NamedFile(name))]);
    }

    /// <summary>
    /// Drops onto the first input, the fresh one the drop zone hands a drop to.
    /// </summary>
    private static async Task Drop(IRenderedComponent<FilesCard> component, params string[] fileNames)
    {
        await component.InvokeAsync(async () => await component.FindComponents<InputFile>()[0].Instance.OnChange.InvokeAsync(Change(fileNames)));
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
            Version = 1,
            VersionCount = 1,
        };
    }

    private sealed class NamedFile(string name) : IBrowserFile
    {
        public string Name { get; } = name;

        public DateTimeOffset LastModified { get; } = DateTimeOffset.UtcNow;

        public long Size => 1;

        public string ContentType => "text/plain";

        public Stream OpenReadStream(long maxAllowedSize = 512000, CancellationToken cancellationToken = default)
        {
            return new MemoryStream("a"u8.ToArray());
        }
    }

    private sealed class Card
    {
        public string Title { get; init; } = "Soubory spisu";

        public string UploadTarget { get; init; } = "ke spisu";

        public bool Compact { get; init; }

        public IReadOnlyList<FileListItem> Files { get; init; } = [];

        public Func<CancellationToken, Task<IReadOnlyList<FileListItem>>>? LoadFiles { get; init; }

        public Func<IBrowserFile, CancellationToken, Task<FileUploadResult>> UploadFile { get; init; } = static (file, _) => Task.FromResult(Stored(file.Name));

        public Func<FileListItem, string, CancellationToken, Task> RenameFile { get; init; } = static (_, _, _) => Task.CompletedTask;

        public Func<FileListItem, IBrowserFile, CancellationToken, Task<FileUploadResult>> UploadFileVersion { get; init; } = static (file, _, _) => Task.FromResult(Stored(file, FileUploadOutcome.NewVersion));

        public Func<FileListItem, CancellationToken, Task<IReadOnlyList<FileVersionItem>>> LoadFileVersions { get; init; } = static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]);

        public Func<FileListItem, int, CancellationToken, Task> DeleteFileVersion { get; init; } = static (_, _, _) => Task.CompletedTask;

        public Func<FileListItem, int?, CancellationToken, Task<Stream>> LoadContent { get; init; } = static (_, _, _) => Task.FromResult<Stream>(new MemoryStream("abc"u8.ToArray()));

        public IRenderedComponent<FilesCard> Render(BunitContext ctx)
        {
            var files = this.Files;
            var loadFiles = this.LoadFiles ?? (_ => Task.FromResult(files));

            return ctx.Render<FilesCard>(parameters => parameters
                .Add(static card => card.Title, this.Title)
                .Add(static card => card.State, new FilesCardState(loadFiles))
                .Add(static card => card.UploadFile, this.UploadFile)
                .Add(static card => card.LoadContent, this.LoadContent)
                .Add(static card => card.UploadFileVersion, this.UploadFileVersion)
                .Add(static card => card.LoadFileVersions, this.LoadFileVersions)
                .Add(static card => card.DeleteFileVersion, this.DeleteFileVersion)
                .Add(static card => card.RenameFile, this.RenameFile)
                .Add(static card => card.DeleteFile, static (_, _) => Task.CompletedTask)
                .Add(static card => card.OwnerGoneError, "spis už neexistuje.")
                .Add(static card => card.UploadTarget, this.UploadTarget)
                .Add(static card => card.Compact, this.Compact));
        }
    }
}

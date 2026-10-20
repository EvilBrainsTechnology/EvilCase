using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalRenderTests
{
    [Test]
    public void APdfIsDrawnByTheLibraryFromTheContentAddressWithoutReadingBytes()
    {
        using var ctx = NewContext(out var blobs, out var previews);

        var loads = 0;
        var file = File("Rozhodnutí.pdf");
        var component = Render(
            ctx,
            [file],
            file.FileId,
            load: (_, _) =>
            {
                loads++;

                return Task.FromResult<Stream>(new MemoryStream());
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-modal-body .ec-preview-pdf"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll(".ec-modal-body .ec-preview-pdf-toolbar"), Has.Count.EqualTo(1), "the PDF toolbar stands in the body, not in the header");
            Assert.That(component.Find(".ec-preview-pdf-page-count").TextContent, Is.EqualTo("z 3"));
            Assert.That(previews.RenderedPdfs, Is.EqualTo([$"/api/files/{file.FileId}/content"]));
            Assert.That(component.FindAll("iframe, object, embed"), Is.Empty, "no browser viewer shows the PDF");
            Assert.That(loads, Is.Zero, "the library requests the file itself");
            Assert.That(blobs.Created, Is.Empty);
        }
    }

    [Test]
    public void APdfTheLibraryCannotOpenShowsTheUnsupportedStateWithTheDownload()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Fails = true;

        var downloaded = new List<string>();
        var file = File("Chráněný.pdf");
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName));

        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici")));

        component.Find(".ec-preview-message .ec-button-primary").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloaded, Is.EqualTo(["Chráněný.pdf"]));
            Assert.That(component.FindAll(".ec-preview-pdf"), Is.Empty);
        }
    }

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

    [TestCase("a.JPG")]
    [TestCase("a.webp")]
    public void ARasterImageShowsInAnImgElementFromTheContentAddress(string fileName)
    {
        using var ctx = NewContext(out var blobs);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("img.ec-preview-image").GetAttribute("src"), Is.EqualTo($"/api/files/{file.FileId}/content"));
            Assert.That(component.FindAll("iframe, object, embed"), Is.Empty);
            Assert.That(blobs.Created, Is.Empty);
        }
    }

    [Test]
    public void AnSvgShowsInAnImgElementFromABlobOfItsOwnType()
    {
        using var ctx = NewContext(out var blobs);

        var file = File("Schéma.svg");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("img.ec-preview-image").GetAttribute("src"), Is.EqualTo("blob:test/0"));
            Assert.That(component.FindAll("iframe, object, embed"), Is.Empty);
            Assert.That(blobs.Created.Single().MediaType, Is.EqualTo("image/svg+xml"));
        }
    }

    [Test]
    public void AudioAndVideoShowInNativeControlsFromTheContentAddress()
    {
        using var ctx = NewContext(out _);

        var audio = File("Nahrávka.mp3");
        var video = File("Záznam.mp4");

        var audioView = Render(ctx, [audio], audio.FileId);
        var videoView = Render(ctx, [video], video.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(audioView.Find("audio.ec-preview-audio").HasAttribute("controls"), Is.True);
            Assert.That(audioView.Find("audio.ec-preview-audio").GetAttribute("src"), Is.EqualTo($"/api/files/{audio.FileId}/content"));
            Assert.That(videoView.Find("video.ec-preview-video").HasAttribute("controls"), Is.True);
            Assert.That(videoView.Find("video.ec-preview-video").GetAttribute("src"), Is.EqualTo($"/api/files/{video.FileId}/content"));
        }
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
    public void ATextShowsAsNumberedPlainLines()
    {
        using var ctx = NewContext(out var blobs);

        var file = File("Poznámky.txt");
        var component = Render(ctx, [file], file.FileId, content: "první\r\n\r\ntřetí\n");

        var lines = component.FindAll(".ec-preview-line");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lines.Select(static line => line.QuerySelector(".ec-preview-line-number")!.TextContent), Is.EqualTo(["1", "2", "3"]));
            Assert.That(lines.Select(static line => line.QuerySelector(".ec-preview-line-text")!.TextContent), Is.EqualTo(["první", "", "třetí"]));
            Assert.That(component.FindAll(".ec-preview-note"), Is.Empty);
            Assert.That(blobs.Created, Is.Empty, "a text is decoded and needs no blob");
        }
    }

    [Test]
    public void AnHtmlFileShowsItsSourceAsTextAndRendersNothing()
    {
        using var ctx = NewContext(out _);

        var file = File("stranka.html");
        var component = Render(ctx, [file], file.FileId, content: "<script>alert(1)</script><b>x</b>");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-line-text").TextContent, Is.EqualTo("<script>alert(1)</script><b>x</b>"));
            Assert.That(component.FindAll(".ec-preview-text script, .ec-preview-text b"), Is.Empty, "the markup is text, never elements");
        }
    }

    [Test]
    public void ALongTextIsCutAndSaysSo()
    {
        using var ctx = NewContext(out _);

        var file = File("Velky.log");
        var component = Render(ctx, [file], file.FileId, content: new string('a', (1024 * 1024) + 10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-note").TextContent, Does.Contain("jen jeho začátek"));
            Assert.That(component.Find(".ec-preview-line-text").TextContent, Has.Length.EqualTo(1024 * 1024));
        }
    }

    [Test]
    public void ATextWithManyLinesIsCutAtTheLineCap()
    {
        using var ctx = NewContext(out _);

        var file = File("Radky.log");
        var component = Render(ctx, [file], file.FileId, content: string.Join('\n', Enumerable.Repeat("x", 6000)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-line"), Has.Count.EqualTo(5000));
            Assert.That(component.FindAll(".ec-preview-note"), Has.Count.EqualTo(1));
        }
    }

    [Test]
    public void AMarkdownFileShowsFormattedAndNeverAsSource()
    {
        using var ctx = NewContext(out var blobs);

        var file = File("Poznámky.md");
        var component = Render(ctx, [file], file.FileId, content: "# Nadpis\n\nText **tučně** <script>alert(1)</script>");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-markdown h1").TextContent, Is.EqualTo("Nadpis"));
            Assert.That(component.Find(".ec-preview-markdown strong").TextContent, Is.EqualTo("tučně"));
            Assert.That(component.FindAll(".ec-preview-markdown script"), Is.Empty, "raw HTML is text, never an element");
            Assert.That(component.FindAll(".ec-preview-line"), Is.Empty);
            Assert.That(blobs.Created, Is.Empty);
        }
    }

    [Test]
    public void ALongMarkdownIsCutAndSaysSo()
    {
        using var ctx = NewContext(out _);

        var file = File("Velky.markdown");
        var component = Render(ctx, [file], file.FileId, content: new string('a', (1024 * 1024) + 10));

        Assert.That(component.Find(".ec-preview-note").TextContent, Does.Contain("jen jeho začátek"));
    }

    [Test]
    public void ADocxShowsTheNoticeAndHandsTheBytesToTheLibrary()
    {
        using var ctx = NewContext(out _, out var previews);

        var file = File("Vyjádření.docx");
        var component = Render(ctx, [file], file.FileId, content: "bajty dokumentu");

        component.WaitForAssertion(() => Assert.That(previews.RenderedDocuments, Is.EqualTo(["bajty dokumentu"])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-note").TextContent, Is.EqualTo("Náhled dokumentu se může od originálu mírně lišit. Přesnou podobu ukáže stažený soubor."));
            Assert.That(component.Find(".ec-preview-document"), Is.Not.Null);
        }
    }

    [Test]
    public void AWorkbookShowsASheetAsATableWithTabsToSwitchBetweenSheets()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Workbook =
        [
            new SheetPreview("Přehled", [["Datum", "Částka"], ["1. 1. 2026", "12"]], 2, 2),
            new SheetPreview("Lhůty", [["Odvolání"]], 1, 1),
        ];

        var file = File("Náklady.xlsx");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-sheet-tab").Select(static tab => tab.TextContent), Is.EqualTo(["Přehled", "Lhůty"]));
            Assert.That(component.FindAll(".ec-preview-sheet-table thead th").Select(static header => header.TextContent), Is.EqualTo(["", "A", "B"]));
            Assert.That(component.FindAll(".ec-preview-sheet-table tbody tr"), Has.Count.EqualTo(2));
            Assert.That(component.FindAll(".ec-preview-sheet-table td").Select(static cell => cell.TextContent), Is.EqualTo(["Datum", "Částka", "1. 1. 2026", "12"]));
            Assert.That(component.FindAll(".ec-preview-note"), Is.Empty);
            Assert.That(previews.RequestedRows, Is.EqualTo([1000]), "the cap is passed to the library so a large sheet is never read whole");
        }

        component.FindAll(".ec-preview-sheet-tab")[1].Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-sheet-table td").Select(static cell => cell.TextContent), Is.EqualTo(["Odvolání"]));
            Assert.That(component.FindAll(".ec-preview-sheet-tab")[1].GetAttribute("aria-selected"), Is.EqualTo("true"));
        }
    }

    [Test]
    public void ASheetLongerThanTheShownRowsSaysSo()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Workbook = [new SheetPreview("List1", [["a"], ["b"]], 5432, 1)];

        var file = File("Velka.ods");
        var component = Render(ctx, [file], file.FileId);

        Assert.That(component.Find(".ec-preview-note").TextContent, Is.EqualTo("Zobrazuje se jen prvních 2 řádků z 5\u00A0432."));
    }

    [Test]
    public void AWorkbookWithoutSheetsSaysSo()
    {
        using var ctx = NewContext(out _);

        var file = File("Prazdny.xls");
        var component = Render(ctx, [file], file.FileId);

        Assert.That(component.Find(".ec-preview-message").TextContent, Is.EqualTo("Sešit neobsahuje žádné listy."));
    }

    [Test]
    public void AnEmailShowsItsHeadersBodyAndAttachments()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Email = new EmailPreview(
            "Jana Nováková <jana@example.org>",
            ["Petr <petr@example.org>", "urad@example.org"],
            [],
            "Doplnění podkladů",
            new DateTimeOffset(2025, 9, 2, 12, 5, 0, TimeSpan.Zero),
            "Dobrý den,\n<b>nic</b>",
            Truncated: false,
            [new EmailAttachment("Doplnění.pdf", 1800)]);

        var file = File("Zpráva.eml");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-email-headers dt").Select(static label => label.TextContent), Is.EqualTo(["Od", "Komu", "Předmět", "Datum"]));
            Assert.That(component.FindAll(".ec-preview-email-headers dd").Select(static value => value.TextContent).Take(3), Is.EqualTo(["Jana Nováková <jana@example.org>", "Petr <petr@example.org>, urad@example.org", "Doplnění podkladů"]));
            Assert.That(component.Find(".ec-preview-email-body").TextContent, Is.EqualTo("Dobrý den,\n<b>nic</b>"));
            Assert.That(component.FindAll(".ec-preview-email-body b"), Is.Empty, "the body is text, never markup");
            Assert.That(component.Find(".ec-preview-email-attachments-title").TextContent, Is.EqualTo("Přílohy (1)"));
            Assert.That(component.Find(".ec-preview-email-attachment-name").TextContent, Is.EqualTo("Doplnění.pdf"));
            Assert.That(component.Find(".ec-preview-email-attachment-size").TextContent, Is.EqualTo("1,8 kB"));
            Assert.That(component.FindAll("img, iframe, script, a[href]"), Is.Empty, "a message loads and runs nothing");
        }
    }

    [Test]
    public void AnEmailWithoutAttachmentsOrCopiesShowsNeitherSection()
    {
        using var ctx = NewContext(out _);

        var file = File("Prazdna.msg");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-email-attachments"), Is.Empty);
            Assert.That(component.FindAll(".ec-preview-email-headers dt").Select(static label => label.TextContent), Is.EqualTo(["Od", "Komu", "Předmět", "Datum"]));
        }
    }

    [TestCase("Foto.heic")]
    [TestCase("Foto.HEIF")]
    public void AConvertedPictureShowsInAnImgElementFromAConvertedBlob(string fileName)
    {
        using var ctx = NewContext(out var blobs, out var previews);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("img.ec-preview-image").GetAttribute("src"), Is.EqualTo("blob:converted/0"));
            Assert.That(previews.Converted, Is.EqualTo(["blob:converted/0"]));
            Assert.That(blobs.Created, Is.Empty, "the original bytes are never shown as a blob");
        }
    }

    [TestCase("Vyjádření.docx")]
    [TestCase("Náklady.xlsx")]
    [TestCase("Zpráva.eml")]
    [TestCase("Zpráva.msg")]
    [TestCase("Foto.heif")]
    public void AFileTheLibraryCannotParseShowsTheUnsupportedStateWithTheDownload(string fileName)
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Fails = true;

        var downloaded = new List<string>();
        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName));

        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici")));

        component.Find(".ec-preview-message .ec-button-primary").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloaded, Is.EqualTo([fileName]));
            Assert.That(component.FindAll(".ec-alert"), Is.Empty, "a file nobody can parse is no loading failure to retry");
        }
    }

    [Test]
    public void SteppingFromAFailedFileToAnotherShowsTheOtherAsUsual()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Fails = true;

        var broken = File("Rozbite.docx");
        var text = File("Poznámky.txt");
        var component = Render(ctx, [broken, text], broken.FileId, content: "řádek");

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-message-title"), Has.Count.EqualTo(1)));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, text.FileId));

        Assert.That(component.Find(".ec-preview-line-text").TextContent, Is.EqualTo("řádek"));
    }

    [TestCase("Doručenka.zfo")]
    [TestCase("Sken.tif")]
    [TestCase("Sken.TIFF")]
    public void AFileWithoutPreviewOffersTheDownloadInstead(string fileName)
    {
        using var ctx = NewContext(out var blobs, out var previews);

        var downloaded = new List<string>();
        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName));

        component.Find(".ec-preview-message .ec-button-primary").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"));
            Assert.That(component.Find(".ec-preview-message .ec-button-primary").TextContent.Trim(), Is.EqualTo("Stáhnout soubor"));
            Assert.That(component.Find(".ec-preview-message .ec-file-icon").ClassList, Does.Contain("ec-file-icon-large"));
            Assert.That(downloaded, Is.EqualTo([fileName]));
            Assert.That(blobs.Created, Is.Empty);
            Assert.That(previews.Converted, Is.Empty);
        }
    }

    [Test]
    public void TheHeaderNamesTheFileItsSizeAndMomentAndTheirPosition()
    {
        using var ctx = NewContext(out _);

        var files = new FileListItem[] { File("a.pdf"), File("b.png"), File("c.txt") };
        var component = Render(ctx, files, files[1].FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("b.png"));
            Assert.That(component.Find(".ec-preview-meta").TextContent, Does.StartWith("1 kB · "));
            Assert.That(component.Find(".ec-preview-position").TextContent, Is.EqualTo("2 z 3"));
            Assert.That(component.Find("dialog").GetAttribute("aria-label"), Is.EqualTo("b.png"));
        }
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
    public void TheHeaderActionsKeepTheirNameWhenTheLabelIsHidden()
    {
        using var ctx = NewContext(out _);

        var file = File("a.pdf");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-modal-header button[aria-label='Přejmenovat'] .ec-preview-action-label").TextContent, Is.EqualTo("Přejmenovat"));
            Assert.That(component.Find(".ec-modal-header button[aria-label='Stáhnout'] .ec-preview-action-label").TextContent, Is.EqualTo("Stáhnout"));
        }
    }

    [Test]
    public void ASingleFileHasNothingToStepTo()
    {
        using var ctx = NewContext(out _);

        var file = File("a.pdf");
        var component = Render(ctx, [file], file.FileId);

        Assert.That(component.FindAll(".ec-preview-nav"), Is.Empty, "a single file shows no pager");
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
    public void AFileThatCannotBeLoadedShowsAnErrorAndRetryLoadsAgain()
    {
        using var ctx = NewContext(out var blobs);

        var calls = 0;
        var file = File("a.svg");
        var component = Render(
            ctx,
            [file],
            file.FileId,
            load: (_, _) =>
            {
                calls++;

                if (calls == 1)
                    throw new HttpRequestException();

                return Task.FromResult<Stream>(new MemoryStream([1]));
            });

        Assert.That(component.Find(".ec-preview-message .ec-alert").TextContent, Does.Contain("Soubor se nepodařilo načíst"));

        component.Find(".ec-preview-message .ec-button").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("img.ec-preview-image"), Has.Count.EqualTo(1));
            Assert.That(blobs.Created, Has.Count.EqualTo(1));
        }
    }

    [Test]
    public async Task RenamingFromTheHeaderTurnsTheNameIntoTheSameInputAndSavesIt()
    {
        await using var ctx = NewContext(out _);

        var renamed = new List<string>();
        var raised = 0;
        var file = File("Rozhodnutí.pdf");
        var component = Render(
            ctx,
            [file],
            file.FileId,
            rename: (_, name, _) =>
            {
                renamed.Add(name);

                return Task.CompletedTask;
            },
            onRenamed: () => raised++);

        var header = component.Find(".ec-modal-header");
        var rename = header.QuerySelectorAll("button").Single(static button => string.Equals(button.TextContent.Trim(), "Přejmenovat", StringComparison.Ordinal));

        await rename.ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("input.ec-file-rename-input").GetAttribute("value"), Is.EqualTo("Rozhodnutí"));
            Assert.That(component.Find(".ec-file-rename-ext").TextContent, Is.EqualTo(".pdf"));
            Assert.That(component.Find(".ec-file-rename").HasAttribute("data-ec-keeps-escape"), Is.True, "Escape cancels the rename and does not close the dialog");
        }

        await component.Find("input.ec-file-rename-input").InputAsync(new ChangeEventArgs { Value = "Nové" });
        await component.Find("input.ec-file-rename-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-file-rename"), Is.Empty));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(renamed, Is.EqualTo(["Nové.pdf"]));
            Assert.That(raised, Is.EqualTo(1));
        }
    }

    [Test]
    public void TheDownloadButtonDownloadsTheShownFile()
    {
        using var ctx = NewContext(out _);

        var downloaded = new List<string>();
        var file = File("a.pdf");
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName));

        component
            .Find(".ec-modal-header")
            .QuerySelectorAll("button")
            .Single(static button => string.Equals(button.TextContent.Trim(), "Stáhnout", StringComparison.Ordinal))
            .Click();

        Assert.That(downloaded, Is.EqualTo(["a.pdf"]));
    }

    [Test]
    public void TheDialogIsTheLargeOne()
    {
        using var ctx = NewContext(out _);

        var file = File("a.pdf");
        var component = Render(ctx, [file], file.FileId);

        Assert.That(component.Find("dialog").ClassList, Does.Contain("ec-modal-large"));
    }

    private static BunitContext NewContext(out StubBlobUrls blobs)
    {
        return NewContext(out blobs, out _);
    }

    private static BunitContext NewContext(out StubBlobUrls blobs, out StubFilePreviews previews)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
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
        Action<Guid>? onFileIdChanged = null)
    {
        var loadContent = load ?? ((_, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content))));
        var renameFile = rename ?? (static (_, _, _) => Task.CompletedTask);

        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, loadContent)
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
        };
    }
}

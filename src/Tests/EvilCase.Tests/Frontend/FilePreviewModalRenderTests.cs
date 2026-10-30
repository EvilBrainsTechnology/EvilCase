using System.Text;
using Bunit;
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
            Assert.That(component.FindAll(".ec-modal-body .ec-preview-toolbar"), Has.Count.EqualTo(1), "the PDF toolbar stands in the body, not in the header");
            Assert.That(component.Find(".ec-preview-toolbar-page-count").TextContent, Is.EqualTo("z 3"));
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

    [TestCase("a.pdf")]
    [TestCase("a.mp3")]
    [TestCase("a.mp4")]
    public void APreviewTheBrowserStreamsKeepsTheSessionFreshUntilAnotherFileTakesOver(string fileName)
    {
        using var ctx = NewContext(out _);

        var streamed = File(fileName);
        var text = File("b.txt");
        var component = Render(ctx, [streamed, text], streamed.FileId);
        var session = ctx.Services.GetRequiredService<StubAuthSession>();

        Assert.That(session.KeptFresh, Has.Count.EqualTo(1));
        Assert.That(session.KeptFresh[0].IsCancellationRequested, Is.False);

        component.Render(parameters => parameters.Add(static modal => modal.FileId, text.FileId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.KeptFresh, Has.Count.EqualTo(1), "a file read as bytes needs no renewal while shown");
            Assert.That(session.KeptFresh[0].IsCancellationRequested, Is.True);
        }
    }

    [Test]
    public async Task ClosingAStreamedPreviewStopsKeepingTheSessionFresh()
    {
        await using var ctx = NewContext(out _);

        var file = File("a.mp4");
        var component = Render(ctx, [file], file.FileId);
        var session = ctx.Services.GetRequiredService<StubAuthSession>();

        await component.Instance.DisposeAsync();

        Assert.That(session.KeptFresh.Single().IsCancellationRequested, Is.True);
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

    [TestCase("data.json", CodeLanguage.Json)]
    [TestCase("faktura.isdoc", CodeLanguage.Xml)]
    [TestCase("config.yml", CodeLanguage.Yaml)]
    [TestCase("stranka.html", CodeLanguage.Html)]
    public void ACodeFileOpensFormattedWithTheFormatSwitchPressed(string fileName, CodeLanguage language)
    {
        using var ctx = NewContext(out _, out var previews);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId, content: "﻿<script>alert(1)</script>");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.RenderedCode, Is.EqualTo([("<script>alert(1)</script>", language, false)]));
            Assert.That(
                component.FindAll(".ec-preview-toolbar button").Select(static button => button.GetAttribute("aria-label")),
                Is.EqualTo(["Zmenšit", "Zvětšit", "100 %", "Formátovat", "Hledat v dokumentu", "Předchozí výskyt", "Další výskyt"]));
            Assert.That(ToolbarButton(component, "Formátovat").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(ToolbarButton(component, "Formátovat").HasAttribute("disabled"), Is.False);
            Assert.That(component.FindAll(".ec-preview-note"), Is.Empty);
            Assert.That(component.Find(".ec-preview-stage [role='document']").ChildElementCount, Is.Zero, "the source is never markup of the page");
        }
    }

    [Test]
    public async Task TheFormatSwitchTurnsBetweenTheOriginalAndTheFormattedText()
    {
        await using var ctx = NewContext(out _, out var previews);

        var file = File("data.json");
        var component = Render(ctx, [file], file.FileId, content: "{\"a\":1}");

        await ToolbarButton(component, "Formátovat").ClickAsync(new MouseEventArgs());

        Assert.That(ToolbarButton(component, "Formátovat").GetAttribute("aria-pressed"), Is.EqualTo("false"));

        await ToolbarButton(component, "Formátovat").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.ShownCode, Is.EqualTo([false, true]));
            Assert.That(previews.RenderedCode, Has.Count.EqualTo(1), "the switch shows the text again without reading the file");
            Assert.That(ToolbarButton(component, "Formátovat").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        }
    }

    [TestCase("data.json", "JSON")]
    [TestCase("data.xml", "XML")]
    [TestCase("faktura.isdoc", "XML")]
    [TestCase("config.yaml", "YAML")]
    [TestCase("stranka.html", "HTML")]
    public void AnInvalidCodeFileShowsTheOriginalWithTheSwitchDisabledAndSaysWhy(string fileName, string language)
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Code = new CodeView(Formatted: false, Invalid: true, LinesCut: false);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-note").TextContent, Is.EqualTo($"Soubor není platný {language}, proto se ukazuje jen originál."));
            Assert.That(ToolbarButton(component, "Formátovat").HasAttribute("disabled"), Is.True);
            Assert.That(ToolbarButton(component, "Formátovat").GetAttribute("aria-pressed"), Is.EqualTo("false"));
        }
    }

    [Test]
    public void ACodeFileThatFailsToFormatForAnotherReasonShowsTheOriginalWithTheSwitchDisabled()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Code = new CodeView(Formatted: false, Invalid: false, LinesCut: false);

        var file = File("data.json");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-note").TextContent, Is.EqualTo("Soubor se nepodařilo naformátovat, ukazuje se originál."));
            Assert.That(ToolbarButton(component, "Formátovat").HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public void ALongCodeFileIsCutShownAsTheOriginalAndSaysSo()
    {
        using var ctx = NewContext(out _, out var previews);

        var file = File("data.json");
        var component = Render(ctx, [file], file.FileId, content: new string('a', (1024 * 1024) + 10));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.RenderedCode.Single().Text, Has.Length.EqualTo(1024 * 1024));
            Assert.That(previews.RenderedCode.Single().Truncated, Is.True);
            Assert.That(component.Find(".ec-preview-note").TextContent, Is.EqualTo("Soubor je delší, zobrazuje se jen jeho začátek a nejde naformátovat."));
            Assert.That(ToolbarButton(component, "Formátovat").HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public void ACodeFileWithManyLinesSaysItIsCut()
    {
        using var ctx = NewContext(out _, out var previews);

        previews.Code = new CodeView(Formatted: true, Invalid: false, LinesCut: true);

        var file = File("data.yaml");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-note").TextContent, Is.EqualTo("Soubor je delší, zobrazuje se jen jeho začátek."));
            Assert.That(ToolbarButton(component, "Formátovat").HasAttribute("disabled"), Is.False, "a file cut at the line cap still formats");
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
    public void ADocxShowsNoNoticeAndHandsTheBytesToTheLibrary()
    {
        using var ctx = NewContext(out _, out var previews);

        var file = File("Vyjádření.docx");
        var component = Render(ctx, [file], file.FileId, content: "bajty dokumentu");

        component.WaitForAssertion(() => Assert.That(previews.RenderedDocuments, Is.EqualTo(["bajty dokumentu"])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-note"), Is.Empty);
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

    [TestCase("Poznámky.txt")]
    [TestCase("Poznámky.md")]
    [TestCase("Zpráva.eml")]
    [TestCase("Zpráva.msg")]
    public void AReflowingPreviewHasTheZoomAndSearchOfThePdfWithoutPagesOrFits(string fileName)
    {
        using var ctx = NewContext(out _, out var previews);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.OpenedTextViews, Is.EqualTo(1));
            Assert.That(component.Find(".ec-modal-body .ec-preview-toolbar").GetAttribute("role"), Is.EqualTo("toolbar"));
            Assert.That(
                component.FindAll(".ec-preview-toolbar button").Select(static button => button.GetAttribute("aria-label")),
                Is.EqualTo(["Zmenšit", "Zvětšit", "100 %", "Hledat v dokumentu", "Předchozí výskyt", "Další výskyt"]));
            Assert.That(component.FindAll(".ec-preview-toolbar-pages"), Is.Empty, "a reflowing text has no pages");
            Assert.That(component.Find(".ec-preview-toolbar-zoom-value").TextContent, Is.EqualTo("100 %"));
            Assert.That(component.FindAll(".ec-preview-stage [role='document']"), Has.Count.EqualTo(1), "the zoom and search apply to the one document element");
        }
    }

    [Test]
    public async Task TheTextToolbarZoomsAndSearchesTheText()
    {
        await using var ctx = NewContext(out _, out var previews);

        var file = File("Poznámky.txt");
        var component = Render(ctx, [file], file.FileId, content: "Lhůta běží.");

        await ToolbarButton(component, "Zvětšit").ClickAsync(new MouseEventArgs());
        await ToolbarButton(component, "Zmenšit").ClickAsync(new MouseEventArgs());
        await ToolbarButton(component, "100 %").ClickAsync(new MouseEventArgs());
        await previews.TextView!.Value.ZoomChanged(125, "");
        await component.Find(".ec-preview-toolbar-search-input").InputAsync(new() { Value = "lhuta" });
        await previews.TextView.Value.MatchesChanged(1, 2, pending: false);
        await component.Find(".ec-preview-toolbar-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter" });
        await component.Find(".ec-preview-toolbar-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Enter", ShiftKey = true });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.ViewZooms, Is.EqualTo([PreviewZoom.In, PreviewZoom.Out, PreviewZoom.Actual]));
            Assert.That(component.Find(".ec-preview-toolbar-zoom-value").TextContent, Is.EqualTo("125 %"));
            Assert.That(component.Find(".ec-preview-toolbar-matches").TextContent, Is.EqualTo("1 z 2"));
            Assert.That(previews.ViewFinds, Is.EqualTo([("lhuta", PreviewFind.New), ("lhuta", PreviewFind.Next), ("lhuta", PreviewFind.Previous)]));
            Assert.That(previews.Finds, Is.Empty, "a text is searched apart from a PDF");
        }

        await component.Find(".ec-preview-toolbar-search-input").KeyDownAsync(new KeyboardEventArgs { Key = "Escape" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.ViewFinds[^1], Is.EqualTo(("", PreviewFind.New)), "an empty query clears the highlights");
            Assert.That(component.Find(".ec-preview-toolbar-search").HasAttribute("data-ec-keeps-escape"), Is.True, "Escape clears the search and leaves the dialog open");
            Assert.That(component.Find(".ec-preview-toolbar-matches").TextContent, Is.Empty);
        }
    }

    [Test]
    public async Task ADocxHasThePagesZoomFitsAndSearch()
    {
        await using var ctx = NewContext(out _, out var previews);

        var file = File("Vyjádření.docx");
        var component = Render(ctx, [file], file.FileId, content: "bajty dokumentu");

        await component.WaitForAssertionAsync(() => Assert.That(previews.TextView, Is.Not.Null));
        await previews.TextView!.Value.ZoomChanged(82, "page-width");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                component.FindAll(".ec-preview-toolbar button").Select(static button => button.GetAttribute("aria-label")),
                Is.EqualTo(["Zmenšit", "Zvětšit", "100 %", "Na šířku", "Celá stránka", "Hledat v dokumentu", "Předchozí výskyt", "Další výskyt"]));
            Assert.That(component.Find(".ec-preview-toolbar-page-input").GetAttribute("value"), Is.EqualTo("1"));
            Assert.That(component.Find(".ec-preview-toolbar-page-count").TextContent, Is.EqualTo("z 4"));
            Assert.That(component.Find(".ec-preview-toolbar-zoom-value").TextContent, Is.EqualTo("82 %"));
            Assert.That(ToolbarButton(component, "Na šířku").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(ToolbarButton(component, "Zvětšit").HasAttribute("disabled"), Is.False);
        }

        await ToolbarButton(component, "Celá stránka").ClickAsync(new MouseEventArgs());
        await ToolbarButton(component, "Na šířku").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-toolbar-search-input").InputAsync(new() { Value = "lhůta" });
        await component.Find(".ec-preview-toolbar-page-input").ChangeAsync(new() { Value = "3" });
        await component.Find(".ec-preview-toolbar-page-input").ChangeAsync(new() { Value = "9" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.ViewZooms, Is.EqualTo([PreviewZoom.PageFit, PreviewZoom.PageWidth]));
            Assert.That(previews.ViewFinds, Is.EqualTo([("lhůta", PreviewFind.New)]));
            Assert.That(previews.ViewPagesGoneTo, Is.EqualTo([3, 4]), "a page past the end is the last one");
        }

        await previews.TextView.Value.PageChanged(2);

        Assert.That(component.Find(".ec-preview-toolbar-page-input").GetAttribute("value"), Is.EqualTo("2"));
    }

    [TestCase("a.txt")]
    [TestCase("a.docx")]
    [TestCase("a.json")]
    public void SteppingToAnotherFileReleasesTheZoomAndSearch(string fileName)
    {
        using var ctx = NewContext(out _, out var previews);

        var first = File(fileName);
        var second = File("b.png");
        var component = Render(ctx, [first, second], first.FileId);

        component.WaitForAssertion(() => Assert.That(previews.TextView, Is.Not.Null));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, second.FileId));

        Assert.That(previews.DisposedViews, Is.EqualTo(1));
    }

    [Test]
    public void SteppingFromACodeFileReleasesItsCode()
    {
        using var ctx = NewContext(out _, out var previews);

        var first = File("a.xml");
        var second = File("b.png");
        var component = Render(ctx, [first, second], first.FileId);

        component.WaitForAssertion(() => Assert.That(previews.RenderedCode, Is.Not.Empty));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, second.FileId));

        Assert.That(previews.DisposedCode, Is.EqualTo(1), "a formatting still running for the left file stops");
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
    [TestCase("data.json")]
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

    [TestCase("Smlouva.doc")]
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
            Assert.That(component.Find(".ec-preview-position").TextContent, Is.EqualTo("Soubor 2 z 3"));
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

    [Test]
    public void ASingleVersionFileHasNoVersionToggleAndLoadsNoVersions()
    {
        using var ctx = NewContext(out _);

        var loads = 0;
        var file = File("a.txt");
        var component = Render(
            ctx,
            [file],
            file.FileId,
            loadVersions: (_, _) =>
            {
                loads++;

                return Task.FromResult<IReadOnlyList<FileVersionItem>>([]);
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("button[aria-label^='Verze']"), Is.Empty);
            Assert.That(component.FindAll(".ec-preview-versions"), Is.Empty);
            Assert.That(loads, Is.Zero);
        }
    }

    [Test]
    public void TheCurrentVersionHidesTheBarUntilTheToggle()
    {
        using var ctx = NewContext(out _);

        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, loadVersions: static (_, _) => Task.FromResult(VersionItems()));

        var toggle = component.Find("button[aria-label='Verze, 3']");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-versions"), Is.Empty);
            Assert.That(toggle.GetAttribute("aria-pressed"), Is.EqualTo("false"));
            Assert.That(toggle.QuerySelector(".ec-preview-versions-count")!.TextContent, Is.EqualTo("3"));
        }

        toggle.Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Normalize(component.Find(".ec-preview-versions-trigger").TextContent), Is.EqualTo($"Verze 3 z 3 · {MomentDisplay.Text(VersionItems()[0].Created)} · jan.priklad@example.cz"));
            Assert.That(component.FindAll(".ec-preview-versions-old"), Is.Empty);
            Assert.That(component.Find("button[aria-label='Verze, 3']").GetAttribute("aria-pressed"), Is.EqualTo("true"));
        }
    }

    [Test]
    public void AnOlderPdfIsDrawnFromItsVersionAddressUnderTheYellowBar()
    {
        using var ctx = NewContext(out _, out var previews);

        var file = Versioned("a.pdf", 3, 3);
        var component = Render(ctx, [file], file.FileId, version: 2, loadVersions: static (_, _) => Task.FromResult(VersionItems()));

        component.WaitForAssertion(() => Assert.That(previews.RenderedPdfs, Is.EqualTo([$"/api/files/{file.FileId}/content?version=2"])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1));
            Assert.That(component.Find(".ec-preview-versions-note").TextContent, Is.EqualTo("Starší verze. Stáhnout stáhne tuto verzi."));
            Assert.That(Normalize(component.Find(".ec-preview-meta").TextContent), Is.EqualTo($"2 kB · {MomentDisplay.Text(VersionItems()[1].Created)}"));
            Assert.That(component.Find("button[aria-label='Verze, 3']").GetAttribute("aria-pressed"), Is.EqualTo("true"));
            Assert.That(component.Find("button[aria-label='Verze, 3']").GetAttribute("aria-disabled"), Is.EqualTo("true"), "only the version bar returns to the current version");
            Assert.That(component.Find("button[aria-label='Verze, 3']").HasAttribute("disabled"), Is.False, "the toggle stays focusable");
            Assert.That(component.Find("button[aria-label='Verze, 3']").GetAttribute("title"), Is.EqualTo("Zobrazena je starší verze"));
            Assert.That(component.Find(".ec-preview-name-row .ec-badge-file-version").TextContent.Trim(), Is.EqualTo("verze 2"));
        }
    }

    [Test]
    public void TheCurrentVersionCarriesNoBadgeAndAnEnabledToggle()
    {
        using var ctx = NewContext(out _);

        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, loadVersions: static (_, _) => Task.FromResult(VersionItems()));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-name-row .ec-badge"), Is.Empty);
            Assert.That(component.Find("button[aria-label='Verze, 3']").HasAttribute("aria-disabled"), Is.False);
        }
    }

    [Test]
    public void AnOlderTextFileIsReadFromItsVersion()
    {
        using var ctx = NewContext(out _);

        var requested = new List<int?>();
        var file = Versioned("a.txt", 3, 3);

        Render(
            ctx,
            [file],
            file.FileId,
            loadVersion: (_, number, _) =>
            {
                requested.Add(number);

                return Task.FromResult<Stream>(new MemoryStream("starší"u8.ToArray()));
            },
            version: 2);

        Assert.That(requested, Is.EqualTo(new int?[] { 2 }));
    }

    [Test]
    public async Task TheDownloadOfAnOlderVersionDownloadsThatVersion()
    {
        await using var ctx = NewContext(out _);

        var downloaded = new List<string>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName), version: 2);

        await component.Find("button[aria-label='Stáhnout']").ClickAsync(new MouseEventArgs());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctx.Services.GetRequiredService<StubFileDownloader>().Downloaded.Single(), Is.EqualTo((file.FileId, (int?)2, "a.txt")));
            Assert.That(downloaded, Is.Empty);
        }
    }

    [Test]
    public async Task ShowingTheCurrentVersionRaisesNull()
    {
        await using var ctx = NewContext(out _);

        var changed = new List<int?>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, version: 2, onVersionChanged: changed.Add);

        await component.Find("button[aria-label='Verze, 3']").ClickAsync(new MouseEventArgs());

        Assert.That(component.FindAll(".ec-preview-versions-old"), Has.Count.EqualTo(1), "the toggle does nothing while an older version is shown");

        await component.Find(".ec-preview-versions .ec-button-quiet").ClickAsync(new MouseEventArgs());

        Assert.That(changed, Is.EqualTo(new int?[] { null }));
    }

    [Test]
    public async Task ReturningToTheCurrentVersionFocusesTheVersionToggle()
    {
        await using var ctx = NewContext(out _);

        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, version: 2);

        // bUnit drops the element reference from the markup once an element renders again, so the id is read first.
        var toggleId = component.Find("button[aria-label='Verze, 3']").GetAttribute("blazor:elementReference");

        await component.Find(".ec-preview-versions .ec-button-quiet").ClickAsync(new MouseEventArgs());
        component.Render(static parameters => parameters.Add(static modal => modal.Version, value: null));

        var focused = ctx.JSInterop.Invocations.Where(static invocation => string.Equals(invocation.Identifier, "Blazor._internal.domWrapper.focus", StringComparison.Ordinal)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-versions"), Is.Empty);
            Assert.That(focused, Is.Not.Empty);
            Assert.That(((ElementReference)focused[^1].Arguments[0]!).Id, Is.EqualTo(toggleId));
        }
    }

    [Test]
    public async Task PickingAVersionFromTheListRaisesItsNumberAndTheCurrentRaisesNull()
    {
        await using var ctx = NewContext(out _);

        var changed = new List<int?>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, onVersionChanged: changed.Add, loadVersions: static (_, _) => Task.FromResult(VersionItems()));

        await component.Find("button[aria-label='Verze, 3']").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-versions .ec-popover-trigger").ClickAsync(new MouseEventArgs());

        var options = component.FindAll(".ec-preview-versions-option");

        Assert.That(options.Select(static option => Normalize(option.QuerySelector(".ec-preview-versions-option-title")!.TextContent)), Is.EqualTo(["Verze 3 aktuální", "Verze 2", "Verze 1"]));

        await options[1].ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-versions .ec-popover-trigger").ClickAsync(new MouseEventArgs());
        await component.FindAll(".ec-preview-versions-option")[0].ClickAsync(new MouseEventArgs());

        Assert.That(changed, Is.EqualTo(new int?[] { 2, null }));
    }

    [Test]
    public async Task ManageVersionsRaisesOnManageVersions()
    {
        await using var ctx = NewContext(out _);

        var managed = new List<Guid>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(ctx, [file], file.FileId, loadVersions: static (_, _) => Task.FromResult(VersionItems()), onManageVersions: item => managed.Add(item.FileId));

        await component.Find("button[aria-label='Verze, 3']").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-versions .ec-popover-trigger").ClickAsync(new MouseEventArgs());
        await component.Find(".ec-preview-versions-manage").ClickAsync(new MouseEventArgs());

        Assert.That(managed, Is.EqualTo([file.FileId]));
    }

    [Test]
    public async Task ANewVersionFromTheHeaderUploadsAndShowsTheCurrent()
    {
        await using var ctx = NewContext(out _);

        var uploaded = new List<string>();
        var changed = new List<int?>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(
            ctx,
            [file],
            file.FileId,
            version: 2,
            onVersionChanged: changed.Add,
            uploadVersion: (_, picked) =>
            {
                uploaded.Add(picked.Name);

                return Task.FromResult<(string?, string?)>((null, "a.txt: nahráno jako verze 4."));
            });

        component.FindComponent<FileVersionPicker>().FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "b.txt"));

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-file-notice").TextContent, Is.EqualTo("a.txt: nahráno jako verze 4.")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(uploaded, Is.EqualTo(["b.txt"]));
            Assert.That(changed, Is.EqualTo(new int?[] { null }));
        }
    }

    [Test]
    public async Task AFailedNewVersionShowsItsReasonInThePreview()
    {
        await using var ctx = NewContext(out _);

        var changed = new List<int?>();
        var file = Versioned("a.txt", 3, 3);
        var component = Render(
            ctx,
            [file],
            file.FileId,
            onVersionChanged: changed.Add,
            uploadVersion: static (_, _) => Task.FromResult<(string?, string?)>(("b.docx: nová verze souboru a.txt musí mít příponu .txt.", null)));

        component.FindComponent<FileVersionPicker>().FindComponent<InputFile>().UploadFiles(InputFileContent.CreateFromText("x", "b.docx"));

        await component.WaitForAssertionAsync(
            () => Assert.That(component.Find(".ec-preview-alert.ec-alert").TextContent, Is.EqualTo("b.docx: nová verze souboru a.txt musí mít příponu .txt.")));

        Assert.That(changed, Is.Empty, "a failed upload leaves the shown version");
    }

    private static string Normalize(string text)
    {
        return string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
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

    private static AngleSharp.Dom.IElement ToolbarButton(IRenderedComponent<FilePreviewModal> component, string label)
    {
        return component.Find($".ec-preview-toolbar button[aria-label='{label}']");
    }

    private static BunitContext NewContext(out StubBlobUrls blobs)
    {
        return NewContext(out blobs, out _);
    }

    private static BunitContext NewContext(out StubBlobUrls blobs, out StubFilePreviews previews)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBrowserHistory>(new StubBrowserHistory());

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        var downloader = new StubFileDownloader();
        ctx.Services.AddSingleton(downloader);
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
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
        Action<Guid>? onFileIdChanged = null,
        Func<FileListItem, int?, CancellationToken, Task<Stream>>? loadVersion = null,
        int? version = null,
        Action<int?>? onVersionChanged = null,
        Func<FileListItem, CancellationToken, Task<IReadOnlyList<FileVersionItem>>>? loadVersions = null,
        Func<FileListItem, IBrowserFile, Task<(string?, string?)>>? uploadVersion = null,
        Action<FileListItem>? onManageVersions = null)
    {
        var loadContent = load ?? ((_, _) => Task.FromResult<Stream>(new MemoryStream(Encoding.UTF8.GetBytes(content))));
        var loadNumbered = loadVersion ?? (async (item, _, token) => await loadContent(item, token));
        var renameFile = rename ?? (static (_, _, _) => Task.CompletedTask);
        var listVersions = loadVersions ?? (static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]));
        var upload = uploadVersion ?? (static (_, _) => Task.FromResult<(string?, string?)>((null, null)));

        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, loadNumbered)
            .Add(static modal => modal.Version, version)
            .Add(static modal => modal.VersionChanged, changed => onVersionChanged?.Invoke(changed))
            .Add(static modal => modal.LoadVersions, listVersions)
            .Add(static modal => modal.UploadVersion, upload)
            .Add(static modal => modal.OnManageVersions, item => onManageVersions?.Invoke(item))
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
            Version = 1,
            VersionCount = 1,
        };
    }
}

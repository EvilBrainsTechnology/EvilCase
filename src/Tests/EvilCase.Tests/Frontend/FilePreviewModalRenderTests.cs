using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalRenderTests
{
    [Test]
    public void APdfShowsInTheBrowsersViewerFromABlobOfTheDetectedType()
    {
        using var ctx = NewContext(out var blobs);

        var file = File("Rozhodnutí.pdf");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blobs.Created, Is.EqualTo([("application/pdf", "blob:test/0")]), "the blob type comes from the extension");
            Assert.That(component.Find("iframe.ec-preview-frame").GetAttribute("src"), Is.EqualTo("blob:test/0"));
            Assert.That(component.Find("iframe.ec-preview-frame").HasAttribute("sandbox"), Is.False);
        }
    }

    [TestCase("a.svg", "image/svg+xml")]
    [TestCase("a.JPG", "image/jpeg")]
    [TestCase("a.webp", "image/webp")]
    public void AnImageShowsInAnImgElementAndNeverInAFrame(string fileName, string mediaType)
    {
        using var ctx = NewContext(out var blobs);

        var file = File(fileName);
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("img.ec-preview-image").GetAttribute("src"), Is.EqualTo("blob:test/0"));
            Assert.That(component.FindAll("iframe, object, embed"), Is.Empty);
            Assert.That(blobs.Created.Single().MediaType, Is.EqualTo(mediaType));
        }
    }

    [Test]
    public void AudioAndVideoShowInNativeControls()
    {
        using var ctx = NewContext(out _);

        var audio = File("Nahrávka.mp3");
        var video = File("Záznam.mp4");

        var audioView = Render(ctx, [audio], audio.FileId);
        var videoView = Render(ctx, [video], video.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(audioView.Find("audio.ec-preview-audio").HasAttribute("controls"), Is.True);
            Assert.That(videoView.Find("video.ec-preview-video").HasAttribute("controls"), Is.True);
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
    public void AFileWithoutPreviewOffersTheDownloadInstead()
    {
        using var ctx = NewContext(out var blobs);

        var downloaded = new List<string>();
        var file = File("Doručenka.zfo");
        var component = Render(ctx, [file], file.FileId, onDownload: item => downloaded.Add(item.FileName));

        component.Find(".ec-preview-message .ec-button-primary").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo("Náhled tohoto typu souboru není k dispozici"));
            Assert.That(component.Find(".ec-preview-message .ec-button-primary").TextContent.Trim(), Is.EqualTo("Stáhnout soubor"));
            Assert.That(component.Find(".ec-preview-message .ec-file-icon").ClassList, Does.Contain("ec-file-icon-large"));
            Assert.That(downloaded, Is.EqualTo(["Doručenka.zfo"]));
            Assert.That(blobs.Created, Is.Empty);
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
            Assert.That(component.Find(".ec-preview-meta").TextContent, Does.StartWith("1 kB · nahráno "));
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
    public void ASingleFileHasNothingToStepTo()
    {
        using var ctx = NewContext(out _);

        var file = File("a.pdf");
        var component = Render(ctx, [file], file.FileId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find("button[aria-label='Další soubor']").HasAttribute("disabled"), Is.True);
            Assert.That(component.Find("button[aria-label='Předchozí soubor']").HasAttribute("disabled"), Is.True);
        }
    }

    [Test]
    public void SwitchingFilesRevokesTheBlobOfTheOneLeft()
    {
        using var ctx = NewContext(out var blobs);

        var files = new FileListItem[] { File("a.pdf"), File("b.png") };
        var component = Render(ctx, files, files[0].FileId);

        component.Render(parameters => parameters.Add(static modal => modal.FileId, files[1].FileId));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(blobs.Revoked, Is.EqualTo(["blob:test/0"]), "the left file's URL is revoked");
            Assert.That(component.Find("img.ec-preview-image").GetAttribute("src"), Is.EqualTo("blob:test/1"));
        }
    }

    [Test]
    public async Task ClosingTheModalRevokesTheBlob()
    {
        await using var ctx = NewContext(out var blobs);

        var file = File("a.pdf");
        var component = Render(ctx, [file], file.FileId);

        await component.Instance.DisposeAsync();

        Assert.That(blobs.Revoked, Is.EqualTo(["blob:test/0"]));
    }

    [Test]
    public void AFileThatCannotBeLoadedShowsAnErrorAndRetryLoadsAgain()
    {
        using var ctx = NewContext(out var blobs);

        var calls = 0;
        var file = File("a.pdf");
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
            Assert.That(component.FindAll("iframe.ec-preview-frame"), Has.Count.EqualTo(1));
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
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        blobs = new StubBlobUrls();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);

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

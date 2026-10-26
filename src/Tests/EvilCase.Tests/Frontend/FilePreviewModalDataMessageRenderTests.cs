using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalDataMessageRenderTests
{
    private const string Unsupported = "Náhled tohoto typu souboru není k dispozici";

    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    private static readonly byte[] TextBytes = Encoding.UTF8.GetBytes("Poučení o odvolání.");

    [Test]
    public void OpeningAnAttachmentShowsItsOwnPreviewUnderAnAttachmentHeader()
    {
        using var ctx = NewContext(out _, out var previews, out _);

        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, static _ => DefaultMessage());

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(2)));
        component.FindAll(".ec-preview-data-message-attachment-open")[0].Click();

        component.WaitForAssertion(() => Assert.That(previews.RenderedPdfContents, Is.EqualTo([PdfBytes])));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.RenderedPdfs, Is.Empty, "the attachment is never requested");
            Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Rozhodnutí.pdf"));
            Assert.That(component.Find(".ec-preview-meta").TextContent, Does.Contain("příloha zprávy Zpráva.zfo"));
            Assert.That(Position(component), Is.EqualTo("příloha 1 z 2"));
            Assert.That(component.FindAll("[aria-label='Zpět na zprávu']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Stáhnout přílohu']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Přejmenovat']"), Is.Empty, "an attachment is not renamed");
            Assert.That(component.FindAll("[aria-label='Další soubor']"), Is.Empty);
        }
    }

    [Test]
    public void TheArrowsStepThroughTheAttachmentsAndWrapAround()
    {
        using var ctx = NewContext(out _, out var previews, out _);

        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, static _ => DefaultMessage());

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(2)));
        component.FindAll(".ec-preview-data-message-attachment-open")[0].Click();
        component.WaitForAssertion(() => Assert.That(previews.RenderedPdfContents, Has.Count.EqualTo(1)));

        component.Find("button[aria-label='Další příloha']").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Poučení.txt"));
                Assert.That(component.Find(".ec-preview-line-text").TextContent, Is.EqualTo("Poučení o odvolání."));
                Assert.That(Position(component), Is.EqualTo("příloha 2 z 2"));
                Assert.That(previews.DisposedPdfs, Is.EqualTo(1));
            }
        });

        component.Find("button[aria-label='Další příloha']").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Rozhodnutí.pdf"));
                Assert.That(Position(component), Is.EqualTo("příloha 1 z 2"));
            }
        });

        component.Find("button[aria-label='Předchozí příloha']").Click();

        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Poučení.txt")));
    }

    [Test]
    public void BackReturnsToTheMessageWithoutLoadingItAgain()
    {
        using var ctx = NewContext(out _, out _, out _);

        var loads = 0;
        var file = File("Zpráva.zfo");
        var component = Render(
            ctx,
            [file],
            file.FileId,
            _ =>
            {
                loads++;

                return DefaultMessage();
            });

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(2)));
        component.FindAll(".ec-preview-data-message-attachment-open")[1].Click();
        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Poučení.txt")));

        component.Find("button[aria-label='Zpět na zprávu']").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".ec-preview-data-message-subject").TextContent, Is.EqualTo("Rozhodnutí o přestupku"));
                Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Zpráva.zfo"));
                Assert.That(component.FindAll("[aria-label='Přejmenovat']"), Has.Count.EqualTo(1));
                Assert.That(loads, Is.EqualTo(1), "the message is read once");
            }
        });
    }

    [Test]
    public void AnOpenedAttachmentDownloadsUnderItsNameFromTheHeader()
    {
        using var ctx = NewContext(out _, out _, out var downloader);

        var downloaded = new List<string>();
        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, static _ => DefaultMessage(), item => downloaded.Add(item.FileName));

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(2)));
        component.FindAll(".ec-preview-data-message-attachment-open")[1].Click();
        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Poučení.txt")));
        component.Find("button[aria-label='Stáhnout přílohu']").Click();

        component.WaitForAssertion(() => Assert.That(downloader.DownloadedContents, Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloader.DownloadedContents[0].FileName, Is.EqualTo("Poučení.txt"));
            Assert.That(downloader.DownloadedContents[0].Content, Is.EqualTo(TextBytes));
            Assert.That(downloaded, Is.Empty, "the stored file is not the one downloaded");
        }
    }

    [Test]
    public void AnAttachmentWithoutPreviewOffersItsOwnDownload()
    {
        using var ctx = NewContext(out _, out _, out var downloader);

        var downloaded = new List<string>();
        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, static _ => SyntheticZfo.Message(("Smlouva.doc", [7, 7])), item => downloaded.Add(item.FileName));

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(1)));
        component.Find(".ec-preview-data-message-attachment-open").Click();
        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));

        component.Find(".ec-preview-message .ec-button-primary").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(downloader.DownloadedContents.Select(static content => content.FileName), Is.EqualTo(["Smlouva.doc"]));
                Assert.That(downloader.DownloadedContents.Select(static content => content.Content), Is.EqualTo([new byte[] { 7, 7 }]));
                Assert.That(downloaded, Is.Empty, "the stored file is not the one downloaded");
            }
        });
    }

    [TestCase("Foto.png", "image/png", "img")]
    [TestCase("Záznam.mp3", "audio/mpeg", "audio")]
    public void APictureOrRecordingAttachmentPlaysFromABlobOfItsTypeThatBackRevokes(string name, string mediaType, string element)
    {
        using var ctx = NewContext(out var blobs, out _, out _);

        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, _ => SyntheticZfo.Message((name, [1, 2])));

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(1)));
        component.Find(".ec-preview-data-message-attachment-open").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(blobs.Created, Is.EqualTo([(mediaType, "blob:test/0")]));
                Assert.That(component.Find($".ec-modal-body {element}").GetAttribute("src"), Is.EqualTo("blob:test/0"));
            }
        });

        component.Find("button[aria-label='Zpět na zprávu']").Click();

        component.WaitForAssertion(() => Assert.That(blobs.Revoked, Is.EqualTo(["blob:test/0"])));
    }

    [Test]
    public void ANestedDataMessageOpensAsAMessageWithItsOwnAttachments()
    {
        using var ctx = NewContext(out _, out _, out _);

        var nested = SyntheticZfo.Message(("Příloha.txt", TextBytes));
        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, _ => SyntheticZfo.Message(("Vnořená.zfo", nested)));

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(1)));
        component.Find(".ec-preview-data-message-attachment-open").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".ec-preview-data-message-subject").TextContent, Is.EqualTo("Rozhodnutí o přestupku"));
                Assert.That(component.Find(".ec-preview-meta").TextContent, Does.Contain("příloha zprávy Zpráva.zfo"));
                Assert.That(component.Find(".ec-preview-data-message-attachment-open").TextContent, Is.EqualTo("Příloha.txt"));
            }
        });

        component.Find(".ec-preview-data-message-attachment-open").Click();

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Příloha.txt"));
                Assert.That(component.Find(".ec-preview-meta").TextContent, Does.Contain("příloha zprávy Vnořená.zfo"));
            }
        });

        component.Find("button[aria-label='Zpět na zprávu']").Click();
        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Vnořená.zfo")));

        component.Find("button[aria-label='Zpět na zprávu']").Click();
        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Zpráva.zfo")));
    }

    [Test]
    public void AZfoThatIsNoIsdsMessageOffersTheDownload()
    {
        using var ctx = NewContext(out _, out _, out _);

        var downloaded = new List<string>();
        var file = File("Formulář.zfo");
        var component = Render(ctx, [file], file.FileId, static _ => "abc"u8.ToArray(), item => downloaded.Add(item.FileName));

        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));
        component.Find(".ec-preview-message .ec-button-primary").Click();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloaded, Is.EqualTo(["Formulář.zfo"]));
            Assert.That(component.FindAll(".ec-alert"), Is.Empty, "a file that is no message is no failure");
        }
    }

    [Test]
    public void AZfoOverTheLimitIsNeverLoaded()
    {
        using var ctx = NewContext(out _, out _, out _);

        var loads = 0;
        var file = File("Velká.zfo", DataMessageZfo.MaxBytes + 1);
        var component = Render(
            ctx,
            [file],
            file.FileId,
            _ =>
            {
                loads++;

                return DefaultMessage();
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(loads, Is.Zero);
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported));
        }
    }

    [Test]
    public void SteppingToAnotherFileLeavesTheAttachment()
    {
        using var ctx = NewContext(out _, out _, out _);

        var message = File("Zpráva.zfo");
        var text = File("Poznámky.txt");
        var component = Render(ctx, [message, text], message.FileId, item => item == message ? DefaultMessage() : TextBytes);

        component.WaitForAssertion(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(2)));
        component.FindAll(".ec-preview-data-message-attachment-open")[1].Click();
        component.WaitForAssertion(() => Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Poučení.txt")));

        component.Render(parameters => parameters.Add(static modal => modal.FileId, text.FileId));

        component.WaitForAssertion(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(component.Find(".ec-preview-name").TextContent, Is.EqualTo("Poznámky.txt"));
                Assert.That(component.FindAll("[aria-label='Zpět na zprávu']"), Is.Empty);
                Assert.That(Position(component), Is.EqualTo("2 z 2"));
            }
        });
    }

    private static string Position(IRenderedComponent<FilePreviewModal> component)
    {
        return string.Join(' ', component.Find(".ec-preview-position").TextContent.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    }

    private static byte[] DefaultMessage()
    {
        return SyntheticZfo.Message(("Rozhodnutí.pdf", PdfBytes), ("Poučení.txt", TextBytes));
    }

    private static BunitContext NewContext(out StubBlobUrls blobs, out StubFilePreviews previews, out StubFileDownloader downloader)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        downloader = new StubFileDownloader();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());

        return ctx;
    }

    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        Func<FileListItem, byte[]> content,
        Action<FileListItem>? onDownload = null)
    {
        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, (item, _) => Task.FromResult<Stream>(new MemoryStream(content(item))))
            .Add(static modal => modal.Rename, static (_, _, _) => Task.CompletedTask)
            .Add(static modal => modal.OnDownload, item => onDownload?.Invoke(item))
            .Add(static modal => modal.Open, value: true));
    }

    private static FileListItem File(string name, long size = 4096)
    {
        return new FileListItem
        {
            FileId = Guid.NewGuid(),
            FileName = name,
            SizeBytes = size,
            Created = new DateTime(2026, 3, 2, 10, 0, 0, DateTimeKind.Utc),
        };
    }
}

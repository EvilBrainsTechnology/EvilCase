using System.Text;
using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Navigation;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalDataMessageRenderTests
{
    private const string Unsupported = "Náhled tohoto typu souboru není k dispozici";

    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    private static readonly byte[] TextBytes = Encoding.UTF8.GetBytes("Poučení o odvolání.");

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
                Assert.That(Position(component), Is.EqualTo("Soubor 2 z 2"));
            }
        });
    }

    [Test]
    public async Task AnOlderMessageIsVerifiedByItsNumber()
    {
        await using var ctx = NewContext(out _, out _, out _);

        var signatures = ctx.Services.GetRequiredService<IFileSignaturesClient>();
        signatures.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignatureResponse { State = FileSignatureState.Valid });

        var file = File("Zpráva.zfo") with { Version = 3, VersionCount = 3 };
        var component = Render(ctx, [file], file.FileId, static _ => DefaultMessage(), version: 2);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-popover-trigger"), Is.Not.Empty));

        await signatures.Received(1).ReadFileSignature(file.FileId, 2, Arg.Any<CancellationToken>());
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
        ctx.Services.AddSingleton<IBrowserHistory>(new StubBrowserHistory());

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        downloader = new StubFileDownloader();
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        ctx.Services.AddSingleton(new StubTextEditors());
        ctx.Services.AddSingleton<ITextEditors>(static services => services.GetRequiredService<StubTextEditors>());
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());
        ctx.Services.AddSingleton(Substitute.For<IFileSignaturesClient>());

        return ctx;
    }

    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        Func<FileListItem, byte[]> content,
        Action<FileListItem>? onDownload = null,
        int? version = null)
    {
        return ctx.Render<FilePreviewModal>(parameters => parameters
            .Add(static modal => modal.Files, files)
            .Add(static modal => modal.FileId, fileId)
            .Add(static modal => modal.LoadContent, (item, _, _) => Task.FromResult<Stream>(new MemoryStream(content(item))))
            .Add(static modal => modal.Version, version)
            .Add(static modal => modal.LoadVersions, static (_, _) => Task.FromResult<IReadOnlyList<FileVersionItem>>([]))
            .Add(static modal => modal.UploadVersion, static (_, _) => Task.FromResult<(string?, string?)>((null, null)))
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
            Version = 1,
            VersionCount = 1,
        };
    }
}

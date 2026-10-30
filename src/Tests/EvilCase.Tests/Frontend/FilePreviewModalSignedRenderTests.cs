using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.App.Navigation;
using EvilBrains.EvilCase.Tests.Files.Signatures;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalSignedRenderTests
{
    private const string Unsupported = "Náhled tohoto typu souboru není k dispozici";

    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    private static readonly byte[] XmlBytes = "<?xml version=\"1.0\"?><a/>"u8.ToArray();

    [Test]
    public async Task ADocumentNamedWithoutAnExtensionShowsAndDownloadsAsTheTypeItsContentShows()
    {
        await using var ctx = NewContext(out _, out var previews, out var downloader, out _);

        previews.DetectedExtension = "xml";
        var file = File("Potvrzení.p7s");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign(XmlBytes, detached: false));
        var editors = ctx.Services.GetRequiredService<StubTextEditors>();

        await component.WaitForAssertionAsync(() =>
            Assert.That(editors.Opened.Select(static opened => (opened.Text, opened.Language)), Is.EqualTo([("<?xml version=\"1.0\"?><a/>", (CodeLanguage?)CodeLanguage.Xml)])));
        await component.Find("button[aria-label='Stáhnout dokument']").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.Detected, Is.EqualTo([XmlBytes]));
            Assert.That(component.Find(".ec-preview-meta").TextContent, Does.Contain("podepsaný dokument Potvrzení.xml"));
            Assert.That(downloader.DownloadedContents.Select(static content => content.FileName), Is.EqualTo(["Potvrzení.xml"]));
        }
    }

    [Test]
    public async Task ADetectionThatFailsKeepsTheDocumentUnderItsName()
    {
        await using var ctx = NewContext(out _, out var previews, out var downloader, out _);

        previews.Fails = true;
        var file = File("Potvrzení.p7s");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign(XmlBytes, detached: false));

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));
        await component.Find("button[aria-label='Stáhnout dokument']").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-signers"), Has.Count.EqualTo(1));
            Assert.That(downloader.DownloadedContents.Select(static content => content.FileName), Is.EqualTo(["Potvrzení"]));
        }
    }

    [Test]
    public async Task ASignedFileThatIsNoCmsOffersTheDownload()
    {
        await using var ctx = NewContext(out _, out _, out _, out var signers);

        var file = File("Smlouva.pdf.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => "abc"u8.ToArray());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));

        await signers
            .DidNotReceiveWithAnyArgs()
            .ListFileSigners(default, default, default);
    }

    [Test]
    public void ASignedFileOverTheLimitIsNeverLoaded()
    {
        using var ctx = NewContext(out _, out _, out _, out _);

        var loads = 0;
        var file = File("Velká.pdf.p7m", FileLimits.MaxSignedFileBytes + 1);
        var component = Render(
            ctx,
            [file],
            file.FileId,
            _ =>
            {
                loads++;

                return SignedFile.Sign(PdfBytes, detached: false);
            });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(loads, Is.Zero);
            Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported));
        }
    }

    private static BunitContext NewContext(
        out StubBlobUrls blobs, out StubFilePreviews previews, out StubFileDownloader downloader, out IFileSignersClient signers)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        ctx.Services.AddSingleton<IBrowserHistory>(new StubBrowserHistory());

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        downloader = new StubFileDownloader();
        signers = Substitute.For<IFileSignersClient>();
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature, CertificateIssuer = "I.CA" };
        signers.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [signer] });
        signers.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignersResponse>().Task);
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
        ctx.Services.AddSingleton(new StubTextEditors());
        ctx.Services.AddSingleton<ITextEditors>(static services => services.GetRequiredService<StubTextEditors>());
        ctx.Services.AddSingleton<IFileDownloader>(downloader);
        ctx.Services.AddSingleton(new StubAuthSession());
        ctx.Services.AddSingleton<IAuthSession>(static services => services.GetRequiredService<StubAuthSession>());
        ctx.Services.AddSingleton(Substitute.For<IFileSignaturesClient>());
        ctx.Services.AddSingleton(signers);

        return ctx;
    }

    private static IRenderedComponent<FilePreviewModal> Render(
        BunitContext ctx,
        IReadOnlyList<FileListItem> files,
        Guid fileId,
        Func<FileListItem, byte[]> content,
        Action<FileListItem>? onDownload = null,
        Action<Guid>? onFileIdChanged = null,
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
            .Add(static modal => modal.FileIdChanged, changed => onFileIdChanged?.Invoke(changed))
            .Add(static modal => modal.Open, value: true));
    }

    [Test]
    public async Task TheTextOfASignedDocumentOffersNoEditing()
    {
        await using var ctx = NewContext(out _, out _, out _, out _);

        var file = File("Poznámka.txt.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign("a"u8.ToArray(), detached: false));
        var editors = ctx.Services.GetRequiredService<StubTextEditors>();

        await component.WaitForAssertionAsync(() => Assert.That(editors.OpenedReadOnly, Is.EqualTo([true])));

        Assert.That(component.FindAll("[aria-label='Upravit']"), Is.Empty, "the content of a signed document is not edited");
    }

    [Test]
    public async Task TheSignersOfAnOlderVersionAreReadFromThatVersion()
    {
        await using var ctx = NewContext(out _, out var previews, out _, out var signers);

        var file = File("Smlouva.pdf.p7m") with { Version = 3, VersionCount = 3 };
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign(PdfBytes, detached: false), version: 2);

        await component.WaitForAssertionAsync(() => Assert.That(previews.RenderedPdfContents, Is.EqualTo([PdfBytes])));
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signer-name"), Has.Count.EqualTo(1)));

        await signers.Received(1).ListFileSigners(file.FileId, 2, Arg.Any<CancellationToken>());
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

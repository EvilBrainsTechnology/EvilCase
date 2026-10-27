using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Auth;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Files;
using EvilBrains.EvilCase.App.Models;
using EvilBrains.EvilCase.Tests.Files.Signatures;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FilePreviewModalSignedRenderTests
{
    private const string Unsupported = "Náhled tohoto typu souboru není k dispozici";

    private static readonly byte[] PdfBytes = "%PDF-1.4"u8.ToArray();

    [Test]
    public async Task AnAttachedSignatureShowsItsDocumentWithTheSignersAbove()
    {
        await using var ctx = NewContext(out _, out var previews, out _, out var signers);

        var file = File("Smlouva.pdf.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign(PdfBytes, detached: false));

        await component.WaitForAssertionAsync(() => Assert.That(previews.RenderedPdfContents, Is.EqualTo([PdfBytes])));
        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signer-name"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.RenderedPdfs, Is.Empty, "the document is read from the signature, never requested");
            Assert.That(component.Find(".ec-preview-signer-name").TextContent, Is.EqualTo("Jan Testovací"));
            Assert.That(component.Find(".ec-preview-meta").TextContent, Does.Contain("podepsaný dokument Smlouva.pdf"));
            Assert.That(component.FindAll("[aria-label='Stáhnout dokument']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Stáhnout .p7m']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Přejmenovat']"), Has.Count.EqualTo(1));
            await signers
                .Received(1)
                .ListFileSigners(file.FileId, Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task TheDocumentDownloadsUnderItsNameWithoutTheSignature()
    {
        await using var ctx = NewContext(out _, out var previews, out var downloader, out _);

        var downloaded = new List<string>();
        var file = File("Smlouva.pdf.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign(PdfBytes, detached: false), item => downloaded.Add(item.FileName));

        await component.WaitForAssertionAsync(() => Assert.That(previews.RenderedPdfContents, Has.Count.EqualTo(1)));
        await component.Find("button[aria-label='Stáhnout dokument']").ClickAsync(new());
        await component.Find("button[aria-label='Stáhnout .p7m']").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(downloader.DownloadedContents.Select(static content => content.FileName), Is.EqualTo(["Smlouva.pdf"]));
            Assert.That(downloader.DownloadedContents.Select(static content => content.Content), Is.EqualTo([PdfBytes]));
            Assert.That(downloaded, Is.EqualTo(["Smlouva.pdf.p7m"]), "the signed file downloads whole under its own name");
        }
    }

    [Test]
    public async Task ADocumentWithoutPreviewOffersItsOwnDownload()
    {
        await using var ctx = NewContext(out _, out _, out var downloader, out _);

        var downloaded = new List<string>();
        var file = File("Smlouva.doc.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign([7, 7], detached: false), item => downloaded.Add(item.FileName));

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));
        await component.Find(".ec-preview-message .ec-button-primary").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-signers"), Has.Count.EqualTo(1), "the signers show without a preview too");
            Assert.That(downloader.DownloadedContents.Select(static content => content.FileName), Is.EqualTo(["Smlouva.doc"]));
            Assert.That(downloader.DownloadedContents.Select(static content => content.Content), Is.EqualTo([new byte[] { 7, 7 }]));
            Assert.That(downloaded, Is.Empty, "the stored file is not the one downloaded");
        }
    }

    [Test]
    public async Task APictureInsideASignatureShowsFromABlobOfItsType()
    {
        await using var ctx = NewContext(out var blobs, out _, out _, out _);

        var file = File("Foto.png.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign([1, 2], detached: false));

        await component.WaitForAssertionAsync(() =>
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(blobs.Created, Is.EqualTo([("image/png", "blob:test/0")]));
                Assert.That(component.Find(".ec-modal-body img").GetAttribute("src"), Is.EqualTo("blob:test/0"));
            }
        });
    }

    [Test]
    public async Task CodeInsideASignatureIsHighlightedInTheLanguageOfItsDocument()
    {
        await using var ctx = NewContext(out _, out var previews, out _, out _);

        var file = File("Faktura.isdoc.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign("<Invoice/>"u8.ToArray(), detached: false));

        await component.WaitForAssertionAsync(() =>
            Assert.That(previews.RenderedCode, Is.EqualTo([("<Invoice/>", CodeLanguage.Xml, false)])));
    }

    [Test]
    public async Task ADetachedSignatureShowsTheSignatureCard()
    {
        await using var ctx = NewContext(out _, out _, out _, out _);

        var file = File("Smlouva.pdf.p7s");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign(PdfBytes, detached: true));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signature-signer"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-signature-title").TextContent, Is.EqualTo("Podpis souboru Smlouva.pdf"));
            Assert.That(component.FindAll("[aria-label='Stáhnout']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Stáhnout dokument']"), Is.Empty, "a detached signature carries no document");
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty);
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
            .ListFileSigners(default, default);
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

    [Test]
    public async Task ASignedFileAttachedToADataMessageOffersOnlyItsDownload()
    {
        await using var ctx = NewContext(out _, out _, out _, out var signers);

        var file = File("Zpráva.zfo");
        var component = Render(ctx, [file], file.FileId, static _ => SyntheticZfo.Message(("Smlouva.pdf.p7m", SignedFile.Sign(PdfBytes, detached: false))));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-data-message-attachment-open"), Has.Count.EqualTo(1)));
        await component.Find(".ec-preview-data-message-attachment-open").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-signature"), Is.Empty);
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty, "an attachment has no stored file whose signers the server reads");
            await signers
                .DidNotReceiveWithAnyArgs()
                .ListFileSigners(default, default);
        }
    }

    private static BunitContext NewContext(
        out StubBlobUrls blobs, out StubFilePreviews previews, out StubFileDownloader downloader, out IFileSignersClient signers)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        blobs = new StubBlobUrls();
        previews = new StubFilePreviews();
        downloader = new StubFileDownloader();
        signers = Substitute.For<IFileSignersClient>();
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature, CertificateIssuer = "I.CA" };
        signers.ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [signer] });
        ctx.Services.AddSingleton<IBlobUrls>(blobs);
        ctx.Services.AddSingleton<IFilePreviews>(previews);
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

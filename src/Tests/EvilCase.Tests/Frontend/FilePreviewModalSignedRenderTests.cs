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
            Assert.That(previews.Detected, Is.Empty, "a known extension decides the type");
            Assert.That(component.Find(".ec-preview-signer-name").TextContent, Is.EqualTo("Jan Testovací"));
            Assert.That(component.Find(".ec-preview-meta").TextContent, Does.Contain("podepsaný dokument Smlouva.pdf"));
            Assert.That(component.FindAll("[aria-label='Stáhnout dokument']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Stáhnout .p7m']"), Has.Count.EqualTo(1));
            Assert.That(component.FindAll("[aria-label='Přejmenovat']"), Has.Count.EqualTo(1));
            await signers
                .Received(1)
                .ListFileSigners(file.FileId, Arg.Any<int?>(), Arg.Any<CancellationToken>());
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

    [TestCase(null)]
    [TestCase("exe")]
    public async Task ADocumentWhoseTypeIsNotKnownFromItsContentOffersItsDownloadUnderItsName(string? detected)
    {
        await using var ctx = NewContext(out _, out var previews, out var downloader, out _);

        previews.DetectedExtension = detected;
        var file = File("Potvrzení.v2.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign("text"u8.ToArray(), detached: false));

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-message-title").TextContent, Is.EqualTo(Unsupported)));
        await component.Find(".ec-preview-message .ec-button-primary").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(previews.Detected, Has.Count.EqualTo(1));
            Assert.That(downloader.DownloadedContents.Select(static content => content.FileName), Is.EqualTo(["Potvrzení.v2"]));
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
        await using var ctx = NewContext(out _, out _, out _, out _);

        var file = File("Faktura.isdoc.p7m");
        var component = Render(ctx, [file], file.FileId, static _ => SignedFile.Sign("<Invoice/>"u8.ToArray(), detached: false));
        var editors = ctx.Services.GetRequiredService<StubTextEditors>();

        await component.WaitForAssertionAsync(() =>
            Assert.That(editors.Opened.Select(static opened => (opened.Text, opened.Language)), Is.EqualTo([("<Invoice/>", (CodeLanguage?)CodeLanguage.Xml)])));

        Assert.That(editors.Formatting, Is.EqualTo([true]));
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
                .ListFileSigners(default, default, default);
        }
    }

    [Test]
    public async Task AFileWithADetachedSignatureShowsThePairedStrip()
    {
        await using var ctx = NewContext(out _, out _, out _, out var signers);

        var (pdf, signature) = DetachedPair();
        ServePair(signers, pdf, signature);
        var component = Render(ctx, [pdf, signature], pdf.FileId, static _ => PdfBytes);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signers-source"), Has.Count.EqualTo(1)));

        await signers
            .Received(1)
            .ListFileSigners(signature.FileId, Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task TheSignatureButtonOpensTheSignature()
    {
        await using var ctx = NewContext(out _, out _, out _, out var signers);

        var opened = new List<Guid>();
        var (pdf, signature) = DetachedPair();
        ServePair(signers, pdf, signature);
        var component = Render(ctx, [pdf, signature], pdf.FileId, static _ => PdfBytes, onFileIdChanged: opened.Add);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signers-source button"), Has.Count.EqualTo(1)));
        await component.Find(".ec-preview-signers-source button").ClickAsync(new());

        Assert.That(opened, Is.EqualTo([signature.FileId]));
    }

    [Test]
    public async Task TheCardOpensTheSignedFile()
    {
        await using var ctx = NewContext(out _, out _, out _, out var signers);

        var opened = new List<Guid>();
        var (pdf, signature) = DetachedPair();
        ServePair(signers, pdf, signature);
        var component = Render(ctx, [pdf, signature], signature.FileId, static _ => SignedFile.Sign(PdfBytes, detached: true), onFileIdChanged: opened.Add);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signature-file"), Has.Count.EqualTo(1)));
        await component.Find(".ec-preview-signature-file").ClickAsync(new());

        Assert.That(opened, Is.EqualTo([pdf.FileId]));
    }

    private static (FileListItem Pdf, FileListItem Signature) DetachedPair()
    {
        var signature = File("Smlouva.pdf.p7s");
        var pdf = File("Smlouva.pdf") with { SignatureFileId = signature.FileId };

        return (pdf, signature);
    }

    private static void ServePair(IFileSignersClient signers, FileListItem pdf, FileListItem signature)
    {
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature, CertificateIssuer = "I.CA" };

        signers.ListFileSigners(signature.FileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [signer], SignedFile = pdf });
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

    [Test]
    public async Task AnOlderVersionShowsNoDetachedSignatureStrip()
    {
        await using var ctx = NewContext(out _, out _, out _, out var signers);

        var signature = File("Smlouva.pdf.p7s");
        var file = File("Smlouva.pdf") with { Version = 3, VersionCount = 3, SignatureFileId = signature.FileId };
        var component = Render(ctx, [file, signature], file.FileId, static _ => PdfBytes, version: 2);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-pdf"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty);
            await signers.DidNotReceiveWithAnyArgs().ListFileSigners(default, default, default);
        }
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

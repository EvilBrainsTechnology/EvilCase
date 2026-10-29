using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSignersRenderTests
{
    private static readonly DateTime Timestamped = new(2026, 10, 12, 8, 14, 0, DateTimeKind.Utc);

    private static readonly DateTime SigningTime = new(2026, 10, 12, 13, 40, 0, DateTimeKind.Utc);

    private static readonly FileSigner Person = new()
    {
        Name = "Jan Testovací",
        Kind = FileSignerKind.QualifiedSignature,
        CertificateIssuer = "I.CA",
        CertificateValidFrom = new DateTime(2026, 2, 3, 12, 0, 0, DateTimeKind.Utc),
        CertificateValidTo = new DateTime(2027, 2, 3, 12, 0, 0, DateTimeKind.Utc),
        Timestamped = Timestamped,
        SigningTime = SigningTime,
    };

    private static readonly FileSigner Company = new()
    {
        Name = "Testovací stavební s.r.o.",
        Kind = FileSignerKind.QualifiedSeal,
        CertificateIssuer = "PostSignum",
        CertificateValidFrom = new DateTime(2026, 2, 3, 12, 0, 0, DateTimeKind.Utc),
        CertificateValidTo = new DateTime(2027, 2, 3, 12, 0, 0, DateTimeKind.Utc),
        SigningTime = SigningTime,
    };

    [Test]
    public async Task WhileTheSignersLoadTheStripSaysSo()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignersResponse>().Task);

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");

        Assert.That(component.Find(".ec-preview-signers-status").TextContent.Trim(), Is.EqualTo("Načítám podpisy…"));
    }

    [Test]
    public async Task TheStripListsEachSignerWithKindAuthorityAndTime()
    {
        await using var ctx = NewContext(out var client);
        var fileId = Guid.CreateVersion7();
        client.ListFileSigners(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });

        var component = Render(ctx, fileId, "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signer"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-signer-name").TextContent, Is.EqualTo("Jan Testovací"));
            Assert.That(component.Find(".ec-preview-signer-summary").TextContent, Is.EqualTo("kvalifikovaný podpis · I.CA · časové razítko " + MomentDisplay.Text(Timestamped)));
            await client
                .Received(1)
                .ListFileSigners(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task AFailedLoadOffersARetry()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(
                static _ => Task.FromException<FileSignersResponse>(new HttpRequestException("offline")),
                static _ => Task.FromResult(new FileSignersResponse { Signers = [Person] }));

        // The retry runs on through the verification, so the verification answers at once.
        client.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signers-status span").TextContent, Is.EqualTo("Podpisy se nepodařilo načíst.")));
        await component.Find(".ec-preview-signers-status button").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signer-name").TextContent, Is.EqualTo("Jan Testovací")));

        await client
            .Received(2)
            .ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AProblemFromTheServerSaysWhyWithoutARetry()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignersResponse { Problem = FileSignersProblem.TooManySigners, Signers = [] });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7s", detached: true);

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signers-status").TextContent, Is.EqualTo("Soubor nese víc než 50 podpisů, podpisy se nezobrazí.")));
        Assert.That(component.FindAll(".ec-preview-signers-status button"), Is.Empty, "the same file answers the same again");
    }

    [Test]
    public async Task ClosingThePreviewCancelsTheLoad()
    {
        await using var ctx = NewContext(out var client);
        var token = CancellationToken.None;
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Do<CancellationToken>(given => token = given)).Returns(new TaskCompletionSource<FileSignersResponse>().Task);

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");
        component.Instance.Dispose();

        Assert.That(token.IsCancellationRequested, Is.True);
    }

    [Test]
    public async Task TheCardNamesTheSignedFileAndEverySigner()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person, Company] });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva o dílo.pdf.p7s", detached: true);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signature-signer"), Has.Count.EqualTo(2)));

        var signers = component.FindAll(".ec-preview-signature-signer");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-signature-title").TextContent, Is.EqualTo("Podpis souboru Smlouva o dílo.pdf"));
            Assert.That(component.Find(".ec-badge-signature").TextContent.Trim(), Is.EqualTo("Samostatný podpis"));
            Assert.That(component.Find(".ec-preview-signature-count").TextContent, Is.EqualTo("Podpisy (2)"));
            Assert.That(signers[0].TextContent, Does.Contain("kvalifikovaný elektronický podpis"));
            Assert.That(signers[0].TextContent, Does.Contain("I.CA, platný"));
            Assert.That(signers[0].TextContent, Does.Contain("Časové razítko"));
            Assert.That(signers[1].TextContent, Does.Contain("Čas podpisu"));
            Assert.That(signers[1].TextContent, Does.Contain("bez časového razítka, uvádí ho podepisující"));
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty, "the card replaces the strip");
        }
    }

    [Test]
    public async Task TheVerdictReplacesTheVerifyingChip()
    {
        await using var ctx = NewContext(out var client);
        var fileId = Guid.CreateVersion7();
        var verification = new TaskCompletionSource<FileSignersResponse>();
        client.ListFileSigners(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });
        client.VerifyFileSigners(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(verification.Task);

        var component = Render(ctx, fileId, "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Ověřuji podpis…")));
        verification.SetResult(new FileSignersResponse { Signers = [Valid(Person)] });

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis platný")));
    }

    [Test]
    public async Task AFailedVerificationRetriesOnlyTheVerification()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });
        client.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(
                static _ => Task.FromException<FileSignersResponse>(new HttpRequestException("offline")),
                static _ => Task.FromResult(new FileSignersResponse { Signers = [Valid(Person)] }));

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit")));
        await component.Find(".ec-popover-trigger").ClickAsync(new());
        await component.Find("[role=dialog] button").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis platný")));

        using (Assert.EnterMultipleScope())
        {
            await client
                .Received(1)
                .ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
            await client
                .Received(2)
                .VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task TheCardOpensItsSignedFile()
    {
        await using var ctx = NewContext(out var client);
        var opened = new List<Guid>();
        var pdf = new FileListItem { FileId = Guid.CreateVersion7(), FileName = "Smlouva o dílo.pdf", SizeBytes = 186_778, Created = SigningTime, Version = 1, VersionCount = 1 };
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person], SignedFile = pdf });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva o dílo.pdf.p7s", detached: true, onOpenFile: opened.Add);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signature-file"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-signature-file-name").TextContent, Is.EqualTo("Smlouva o dílo.pdf"));
            Assert.That(component.Find(".ec-preview-signature-file-size").TextContent, Is.EqualTo(FileSizeDisplay.Text(186_778)));
        }

        await component.Find(".ec-preview-signature-file").ClickAsync(new());

        Assert.That(opened, Is.EqualTo([pdf.FileId]));
    }

    [Test]
    public async Task TheCardWithoutItsSignedFileSaysSo()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignersResponse { Signers = [Person], SignedFileProblem = DetachedSignatureProblem.Missing });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva o dílo.pdf.p7s", detached: true);

        await component.WaitForAssertionAsync(() => Assert.That(
            component.Find(".ec-preview-signature-file-section .ec-preview-signers-status").TextContent,
            Is.EqualTo("Soubor Smlouva o dílo.pdf mezi soubory není. Nahrajte ho pod tímto jménem.")));
    }

    [Test]
    public async Task TheCardShowsAnUnverifiableReasonWithItsSigner()
    {
        await using var ctx = NewContext(out var client);
        var untrusted = Person with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.UntrustedIssuer };
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person, Company] });
        client.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [untrusted, Valid(Company)] });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva o dílo.pdf.p7s", detached: true);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signature-problem"), Has.Count.EqualTo(1)));

        var signers = component.FindAll(".ec-preview-signature-signer");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signers[0].QuerySelector(".ec-signature-title")!.TextContent.Trim(), Is.EqualTo("Certifikát nevydala česká kvalifikovaná autorita."));
            Assert.That(signers[0].QuerySelectorAll("button"), Is.Empty, "another authority answers the same again");
            Assert.That(signers[1].TextContent, Does.Contain("Odvolání"));
            Assert.That(signers[1].QuerySelector(".ec-badge")!.TextContent.Trim(), Is.EqualTo("Podpis platný"));
        }
    }

    [Test]
    public async Task ThePairedStripShowsForItsSignedFile()
    {
        await using var ctx = NewContext(out var client);
        var opened = new List<Guid>();
        var signatureId = Guid.CreateVersion7();
        var pdf = new FileListItem { FileId = Guid.CreateVersion7(), FileName = "Smlouva o dílo.pdf", SizeBytes = 1, Created = SigningTime, Version = 1, VersionCount = 1 };
        client.ListFileSigners(signatureId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person], SignedFile = pdf });

        var component = Render(ctx, signatureId, "Smlouva o dílo.pdf.p7s", signedFileId: pdf.FileId, onOpenFile: opened.Add);

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signers-source"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-signers-source span").TextContent, Is.EqualTo("Podepsáno samostatným podpisem"));
            Assert.That(component.Find(".ec-preview-signers-source button").TextContent.Trim(), Is.EqualTo("Smlouva o dílo.pdf.p7s"));
        }

        await component.Find(".ec-preview-signers-source button").ClickAsync(new());

        Assert.That(opened, Is.EqualTo([signatureId]));
    }

    [Test]
    public async Task APairedStripWhoseSignersFailToLoadOffersARetry()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<FileSignersResponse>(new HttpRequestException("offline")));

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva o dílo.pdf.p7s", signedFileId: Guid.CreateVersion7());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signers-status span").TextContent, Is.EqualTo("Podpisy se nepodařilo načíst.")));
        Assert.That(component.FindAll(".ec-preview-signers-status button"), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task AnAttachedSignatureLeavesNoPairedStrip()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m", signedFileId: Guid.CreateVersion7());

        await client
            .Received(1)
            .ListFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll(".ec-preview-signers"), Is.Empty, "a signature that signs no other file shows no strip there");
            await client
                .DidNotReceiveWithAnyArgs()
                .VerifyFileSigners(default, default, default);
        }
    }

    private static string Chip(IRenderedComponent<FileSigners> component)
    {
        return component.Find(".ec-preview-signer .ec-badge").TextContent.Trim();
    }

    private static FileSigner Valid(FileSigner signer)
    {
        return signer with { State = FileSignatureState.Valid, RevocationChecked = SigningTime };
    }

    private static BunitContext NewContext(out IFileSignersClient client)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        client = Substitute.For<IFileSignersClient>();
        client.VerifyFileSigners(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignersResponse>().Task);
        ctx.Services.AddSingleton(client);

        return ctx;
    }

    private static IRenderedComponent<FileSigners> Render(
        BunitContext ctx,
        Guid fileId,
        string fileName,
        bool detached = false,
        Guid? signedFileId = null,
        Action<Guid>? onOpenFile = null)
    {
        return ctx.Render<FileSigners>(parameters => parameters
            .Add(static signers => signers.FileId, fileId)
            .Add(static signers => signers.FileName, fileName)
            .Add(static signers => signers.Detached, detached)
            .Add(static signers => signers.SignedFileId, signedFileId)
            .Add(static signers => signers.OnOpenFile, opened => onOpenFile?.Invoke(opened)));
    }
}

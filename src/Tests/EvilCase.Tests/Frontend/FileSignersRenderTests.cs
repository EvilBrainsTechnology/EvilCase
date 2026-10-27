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
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignersResponse>().Task);

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");

        Assert.That(component.Find(".ec-preview-signers-status").TextContent.Trim(), Is.EqualTo("Načítám podpisy…"));
    }

    [Test]
    public async Task TheStripListsEachSignerWithKindAuthorityAndTime()
    {
        await using var ctx = NewContext(out var client);
        var fileId = Guid.CreateVersion7();
        client.ListFileSigners(fileId, Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });

        var component = Render(ctx, fileId, "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signer"), Has.Count.EqualTo(1)));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-preview-signer-name").TextContent, Is.EqualTo("Jan Testovací"));
            Assert.That(component.Find(".ec-preview-signer-summary").TextContent, Is.EqualTo("kvalifikovaný podpis · I.CA · časové razítko " + MomentDisplay.Text(Timestamped)));
            await client
                .Received(1)
                .ListFileSigners(fileId, Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task AFailedLoadOffersARetry()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(
                static _ => Task.FromException<FileSignersResponse>(new HttpRequestException("offline")),
                static _ => Task.FromResult(new FileSignersResponse { Signers = [Person] }));

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signers-status span").TextContent, Is.EqualTo("Podpisy se nepodařilo načíst.")));
        await component.Find(".ec-preview-signers-status button").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signer-name").TextContent, Is.EqualTo("Jan Testovací")));

        await client
            .Received(2)
            .ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task AProblemFromTheServerShowsAsAFailure()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignersResponse { Problem = FileSignersProblem.NotSigned, Signers = [] });

        var component = Render(ctx, Guid.CreateVersion7(), "Smlouva.pdf.p7m");

        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-preview-signers-status span").TextContent, Is.EqualTo("Podpisy se nepodařilo načíst.")));
    }

    [Test]
    public async Task TheCardNamesTheSignedFileAndEverySigner()
    {
        await using var ctx = NewContext(out var client);
        client.ListFileSigners(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person, Company] });

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

    private static BunitContext NewContext(out IFileSignersClient client)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        client = Substitute.For<IFileSignersClient>();
        ctx.Services.AddSingleton(client);

        return ctx;
    }

    private static IRenderedComponent<FileSigners> Render(BunitContext ctx, Guid fileId, string fileName, bool detached = false)
    {
        return ctx.Render<FileSigners>(parameters => parameters
            .Add(static signers => signers.FileId, fileId)
            .Add(static signers => signers.FileName, fileName)
            .Add(static signers => signers.Detached, detached));
    }
}

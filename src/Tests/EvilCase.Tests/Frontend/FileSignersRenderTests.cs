using Bunit;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
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
    public async Task TheVersionIsPassedToTheServer()
    {
        await using var ctx = NewContext(out var client);
        var fileId = Guid.CreateVersion7();
        client.ListFileSigners(fileId, 2, Arg.Any<CancellationToken>()).Returns(new FileSignersResponse { Signers = [Person] });

        var component = ctx.Render<FileSigners>(parameters => parameters
            .Add(static signers => signers.FileId, fileId)
            .Add(static signers => signers.FileName, "Smlouva.pdf.p7m")
            .Add(static signers => signers.Version, 2));

        await component.WaitForAssertionAsync(() => Assert.That(component.FindAll(".ec-preview-signer"), Has.Count.EqualTo(1)));

        await client.Received(1).ListFileSigners(fileId, 2, Arg.Any<CancellationToken>());
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

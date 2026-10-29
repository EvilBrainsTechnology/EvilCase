using System.Net;
using Bunit;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using Microsoft.Extensions.DependencyInjection;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class DataMessageSignatureRenderTests
{
    private static readonly FileSignatureResponse Valid = new()
    {
        State = FileSignatureState.Valid,
        CertificateIssuer = "Testovací kvalifikovaná CA",
        CertificateValidFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        CertificateValidTo = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        Timestamped = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc),
        RevocationChecked = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc),
    };

    [Test]
    public async Task WhileTheVerdictLoadsTheChipSaysSo()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(new TaskCompletionSource<FileSignatureResponse>().Task);

        var component = Render(ctx);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Chip(component), Is.EqualTo("Ověřuji podpis…"));
            Assert.That(component.FindAll(".ec-signature-loading .ec-icon"), Has.Count.EqualTo(1), "the loading chip turns a spinner");
        }
    }

    [Test]
    public async Task TheVerdictOfTheFileInTheParameterIsAskedFor()
    {
        await using var ctx = NewContext(out var client);
        var fileId = Guid.CreateVersion7();
        client.ReadFileSignature(fileId, Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Valid);

        var component = Render(ctx, fileId);

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis ISDS platný")));
    }

    [Test]
    public async Task AValidSignatureOpensItsDetails()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>()).Returns(Valid);

        var component = Render(ctx);
        await component.WaitForAssertionAsync(() => Assert.That(component.Find(".ec-badge").ClassList, Does.Contain("ec-badge-active")));

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        var details = component.Find("[role=dialog]").TextContent;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(details, Does.Contain("Podepsal"));
            Assert.That(details, Does.Contain("Informační systém datových schránek"));
            Assert.That(details, Does.Contain("Testovací kvalifikovaná CA"));
            Assert.That(details, Does.Contain("PostSignum, platný k času razítka"));
            Assert.That(details, Does.Contain("Časové razítko"));
            Assert.That(details, Does.Contain("Odvolání"));
            Assert.That(details, Does.Contain("neodvolán, ověřeno"));
            Assert.That(component.Find("[role=dialog]").ClassList, Is.EquivalentTo(["ec-popover"]));
            Assert.That(component.Find(".ec-popover-trigger").GetAttribute("aria-expanded"), Is.EqualTo("true"));
        }
    }

    [Test]
    public async Task AnInvalidSignatureNamesItsReason()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignatureResponse { State = FileSignatureState.Invalid, Problem = FileSignatureProblem.ContentMismatch });

        var component = Render(ctx);
        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis neplatný")));

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-badge").ClassList, Does.Contain("ec-badge-danger"));
            Assert.That(component.Find(".ec-signature-reason").TextContent, Does.Contain("Obsah zprávy neodpovídá podpisu"));
            Assert.That(component.Find("[role=dialog]").ClassList, Does.Contain("ec-popover-danger"));
            Assert.That(component.FindAll("[role=dialog] button"), Is.Empty, "an invalid verdict offers no retry");
        }
    }

    [Test]
    public async Task AnUnverifiableSignatureRetriesOnRequest()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignatureResponse { State = FileSignatureState.Unverifiable }, Valid);

        var component = Render(ctx);
        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit")));

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        Assert.That(
            component.Find(".ec-signature-text").TextContent.Trim(),
            Is.EqualTo("Kontrola odvolání certifikátu u PostSignum není dostupná. Podpis i řetězec certifikátů jsou v pořádku."));

        await component.Find("[role=dialog] button").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis ISDS platný")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.FindAll("[role=dialog]"), Is.Empty, "the retry closes the details");
            await client
                .Received(2)
                .ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
        }
    }

    [Test]
    public async Task AFailedLoadIsUnverifiableWithARetry()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(static _ => Task.FromException<FileSignatureResponse>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)));

        var component = Render(ctx);
        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit")));

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-signature-text").TextContent.Trim(), Is.EqualTo("Výsledek ověření podpisu se nepodařilo načíst."), "a failed load claims nothing about the signature");
            Assert.That(component.Find("[role=dialog]").ClassList, Does.Contain("ec-popover-waiting"));
            Assert.That(component.Find("[role=dialog] button").TextContent.Trim(), Is.EqualTo("Zkusit znovu"));
        }
    }

    [Test]
    public async Task TheVersionIsPassedToTheServer()
    {
        await using var ctx = NewContext(out var client);
        var fileId = Guid.CreateVersion7();
        client.ReadFileSignature(fileId, 2, Arg.Any<CancellationToken>()).Returns(Valid);

        var component = ctx.Render<DataMessageSignature>(parameters => parameters
            .Add(static signature => signature.FileId, fileId)
            .Add(static signature => signature.Version, 2));

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.Not.EqualTo("Ověřuji podpis…")));

        await client.Received(1).ReadFileSignature(fileId, 2, Arg.Any<CancellationToken>());
    }

    private static string Chip(IRenderedComponent<DataMessageSignature> component)
    {
        return component.Find(".ec-badge").TextContent.Trim();
    }

    private static BunitContext NewContext(out IFileSignaturesClient client)
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        client = Substitute.For<IFileSignaturesClient>();
        ctx.Services.AddSingleton(client);

        return ctx;
    }

    private static IRenderedComponent<DataMessageSignature> Render(BunitContext ctx, Guid? fileId = null)
    {
        return ctx.Render<DataMessageSignature>(parameters => parameters.Add(static signature => signature.FileId, fileId ?? Guid.CreateVersion7()));
    }
}

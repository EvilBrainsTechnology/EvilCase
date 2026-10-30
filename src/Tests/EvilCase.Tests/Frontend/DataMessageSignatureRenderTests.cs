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
    [Test]
    public async Task AFailedLoadIsUnverifiableAndRetries()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(
                static _ => Task.FromException<FileSignatureResponse>(new ApiException(HttpStatusCode.InternalServerError, responseBody: null)),
                static _ => Task.FromResult(new FileSignatureResponse { State = FileSignatureState.Valid }));

        var component = Render(ctx);
        await component.WaitForAssertionAsync(
            () => Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit"), "a failed load is never shown as valid"));

        await component.Find(".ec-popover-trigger").ClickAsync(new());
        await component.Find("[role=dialog] button").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis ISDS platný")));
    }

    [Test]
    public async Task AnInvalidSignatureOffersNoRetry()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignatureResponse { State = FileSignatureState.Invalid, Problem = FileSignatureProblem.ContentMismatch });

        var component = Render(ctx);
        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis neplatný")));

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        Assert.That(component.FindAll("[role=dialog] button"), Is.Empty, "an invalid verdict offers no retry");
    }

    [Test]
    public async Task AnUnverifiableSignatureRetriesOnRequest()
    {
        await using var ctx = NewContext(out var client);
        client.ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(new FileSignatureResponse { State = FileSignatureState.Unverifiable }, new FileSignatureResponse { State = FileSignatureState.Valid });

        var component = Render(ctx);
        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit")));

        await component.Find(".ec-popover-trigger").ClickAsync(new());
        await component.Find("[role=dialog] button").ClickAsync(new());

        await component.WaitForAssertionAsync(() => Assert.That(Chip(component), Is.EqualTo("Podpis ISDS platný")));
        await client.Received(2).ReadFileSignature(Arg.Any<Guid>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
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

    private static IRenderedComponent<DataMessageSignature> Render(BunitContext ctx)
    {
        return ctx.Render<DataMessageSignature>(static parameters => parameters.Add(static signature => signature.FileId, Guid.CreateVersion7()));
    }
}

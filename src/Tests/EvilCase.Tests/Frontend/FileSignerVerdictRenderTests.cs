using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSignerVerdictRenderTests
{
    private static readonly FileSigner Person = new()
    {
        Name = "Jan Testovací",
        Kind = FileSignerKind.QualifiedSignature,
        CertificateIssuer = "I.CA",
        Timestamped = new DateTime(2026, 10, 12, 8, 14, 0, DateTimeKind.Utc),
    };

    [Test]
    public async Task AFailedLoadIsUnverifiableWithARetry()
    {
        await using var ctx = NewContext();
        var retries = 0;
        var component = Render(ctx, Person, () => retries++);

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit"), "a failed load is never shown as valid");

        await component.Find("[role=dialog] button").ClickAsync(new());

        Assert.That(retries, Is.EqualTo(1));
    }

    [Test]
    public async Task AnInvalidSignatureOffersNoRetry()
    {
        await using var ctx = NewContext();
        var component = Render(ctx, Person with { State = FileSignatureState.Invalid, Problem = FileSignerProblem.ContentMismatch });

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Chip(component), Is.EqualTo("Podpis neplatný"));
            Assert.That(component.FindAll("[role=dialog] button"), Is.Empty, "an invalid verdict answers the same again");
        }
    }

    [Test]
    public async Task AnUntrustedIssuerIsUnverifiableWithoutARetry()
    {
        await using var ctx = NewContext();
        var signer = Person with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.UntrustedIssuer };
        var component = Render(ctx, signer);

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit"));
            Assert.That(component.FindAll("[role=dialog] button"), Is.Empty, "another authority answers the same again");
        }
    }

    [Test]
    public async Task AnUnreachableRevocationCheckOffersARetry()
    {
        await using var ctx = NewContext();
        var retries = 0;
        var signer = Person with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.RevocationUnknown };
        var component = Render(ctx, signer, () => retries++);

        await component.Find(".ec-popover-trigger").ClickAsync(new());
        await component.Find("[role=dialog] button").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Chip(component), Is.EqualTo("Podpis nelze ověřit"));
            Assert.That(retries, Is.EqualTo(1));
        }
    }

    private static string Chip(IRenderedComponent<FileSignerVerdict> component)
    {
        return component.Find(".ec-badge").TextContent.Trim();
    }

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    private static IRenderedComponent<FileSignerVerdict> Render(BunitContext ctx, FileSigner signer, Action? onRetry = null)
    {
        return ctx.Render<FileSignerVerdict>(parameters => parameters
            .Add(static verdict => verdict.Signer, signer)
            .Add(static verdict => verdict.SignatureFileName, "Smlouva o dílo.pdf.p7s")
            .Add(static verdict => verdict.OnRetry, onRetry ?? (static () => { })));
    }
}

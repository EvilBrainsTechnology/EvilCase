using Bunit;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Components;
using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSignerVerdictRenderTests
{
    private const string SignatureFileName = "Smlouva o dílo.pdf.p7s";

    private static readonly DateTime Timestamped = new(2026, 10, 12, 8, 14, 0, DateTimeKind.Utc);

    private static readonly DateTime SigningTime = new(2026, 10, 12, 13, 40, 0, DateTimeKind.Utc);

    private static readonly DateTime RevocationChecked = new(2026, 10, 27, 10, 5, 0, DateTimeKind.Utc);

    private static readonly FileSigner Person = new()
    {
        Name = "Jan Testovací",
        Kind = FileSignerKind.QualifiedSignature,
        CertificateIssuer = "I.CA",
        Timestamped = Timestamped,
        SigningTime = SigningTime,
    };

    [Test]
    public async Task WhileVerifyingTheChipSaysSo()
    {
        await using var ctx = NewContext();

        var component = Render(ctx, Person, verifying: true);

        Assert.That(component.Find(".ec-signature-loading .ec-badge").TextContent.Trim(), Is.EqualTo("Ověřuji podpis…"));
    }

    [Test]
    public async Task AValidTimestampedSignatureShowsItsDetails()
    {
        await using var ctx = NewContext();
        var component = Render(ctx, Person with { State = FileSignatureState.Valid, RevocationChecked = RevocationChecked });

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        var details = component.Find("[role=dialog]").TextContent;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-badge").TextContent.Trim(), Is.EqualTo("Podpis platný"));
            Assert.That(details, Does.Contain("I.CA, platný k času razítka"));
            Assert.That(details, Does.Contain("Časové razítko"));
            Assert.That(details, Does.Contain("neodvolán, ověřeno " + MomentDisplay.Text(RevocationChecked)));
            Assert.That(component.FindAll("[role=dialog] .ec-signature-note"), Is.Empty);
        }
    }

    [Test]
    public async Task AValidSignatureWithoutATimestampIsJudgedToday()
    {
        await using var ctx = NewContext();
        var signer = Person with { CertificateIssuer = "PostSignum", Timestamped = null, State = FileSignatureState.Valid, RevocationChecked = RevocationChecked };
        var component = Render(ctx, signer);

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        var details = component.Find("[role=dialog]").TextContent;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(details, Does.Contain("PostSignum, platný ke dni ověření"));
            Assert.That(details, Does.Contain("Čas podpisu"));
            Assert.That(component.Find(".ec-signature-note").TextContent, Is.EqualTo("Bez časového razítka. Čas uvádí podepisující, podpis je ověřen k dnešku."));
        }
    }

    [Test]
    public async Task AnInvalidSignatureNamesItsReason()
    {
        await using var ctx = NewContext();
        var component = Render(ctx, Person with { State = FileSignatureState.Invalid, Problem = FileSignerProblem.ContentMismatch });

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-badge").TextContent.Trim(), Is.EqualTo("Podpis neplatný"));
            Assert.That(component.Find("[role=dialog]").ClassList, Does.Contain("ec-popover-danger"));
            Assert.That(component.Find(".ec-signature-reason").TextContent, Is.EqualTo("Soubor Smlouva o dílo.pdf neodpovídá podpisu."));
        }
    }

    [Test]
    public async Task AnUntrustedIssuerShowsSignerAndIssuer()
    {
        await using var ctx = NewContext();
        var signer = Person with { CertificateIssuer = "Testovací CA", State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.UntrustedIssuer };
        var component = Render(ctx, signer);

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-badge").TextContent.Trim(), Is.EqualTo("Podpis nelze ověřit"));
            Assert.That(component.Find(".ec-signature-title").TextContent, Is.EqualTo("Certifikát nevydala česká kvalifikovaná autorita."));
            Assert.That(component.Find(".ec-signature-note").TextContent, Is.EqualTo("Obsah odpovídá podpisu. Kdo certifikát vydal, aplikace neověří."));
            Assert.That(component.Find("[role=dialog]").TextContent, Does.Contain("Vydavatel"));
            Assert.That(component.FindAll("[role=dialog] button"), Is.Empty, "another authority answers the same again");
        }
    }

    [Test]
    public async Task AnUnreachableRevocationCheckOffersARetry()
    {
        await using var ctx = NewContext();
        var retries = 0;
        var signer = Person with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.RevocationUnknown };
        var component = Render(ctx, signer, onRetry: () => retries++);

        await component.Find(".ec-popover-trigger").ClickAsync(new());
        await component.Find("[role=dialog] button").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(retries, Is.EqualTo(1));
            Assert.That(component.FindAll("[role=dialog]"), Is.Empty, "the retry closes the details");
        }
    }

    [Test]
    public async Task AFailedLoadIsUnverifiableWithARetry()
    {
        await using var ctx = NewContext();
        var component = Render(ctx, Person);

        await component.Find(".ec-popover-trigger").ClickAsync(new());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(component.Find(".ec-badge").TextContent.Trim(), Is.EqualTo("Podpis nelze ověřit"));
            Assert.That(component.Find(".ec-signature-text").TextContent.Trim(), Is.EqualTo("Výsledek ověření podpisu se nepodařilo načíst."), "a failed load claims nothing about the signature");
            Assert.That(component.Find("[role=dialog]").ClassList, Does.Contain("ec-popover-waiting"));
            Assert.That(component.Find("[role=dialog] button").TextContent.Trim(), Is.EqualTo("Zkusit znovu"));
        }
    }

    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        return ctx;
    }

    private static IRenderedComponent<FileSignerVerdict> Render(BunitContext ctx, FileSigner signer, bool verifying = false, Action? onRetry = null)
    {
        return ctx.Render<FileSignerVerdict>(parameters => parameters
            .Add(static verdict => verdict.Signer, signer)
            .Add(static verdict => verdict.Verifying, verifying)
            .Add(static verdict => verdict.SignatureFileName, SignatureFileName)
            .Add(static verdict => verdict.OnRetry, onRetry ?? (static () => { })));
    }
}

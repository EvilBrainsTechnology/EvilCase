using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSignerDisplayTests
{
    private static readonly DateTime Timestamped = new(2026, 10, 12, 8, 14, 0, DateTimeKind.Utc);

    private static readonly DateTime SigningTime = new(2026, 10, 12, 13, 40, 0, DateTimeKind.Utc);

    [Test]
    public void EveryKindHasItsOwnText()
    {
        var kinds = Enum.GetValues<FileSignerKind>();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(kinds.Select(FileSignerDisplay.Kind), Is.Unique, "each kind of signature reads differently");
            Assert.That(kinds.Select(FileSignerDisplay.ShortKind), Is.Unique, "each kind of signature reads differently in short");
        }
    }

    [Test]
    public void TheSummaryNamesTheTimestamp()
    {
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature, CertificateIssuer = "I.CA", Timestamped = Timestamped, SigningTime = SigningTime };

        Assert.That(FileSignerDisplay.Summary(signer), Is.EqualTo("kvalifikovaný podpis · I.CA · časové razítko " + MomentDisplay.Text(Timestamped)));
    }

    [Test]
    public void TheSummaryFallsBackToTheStatedSigningTime()
    {
        var signer = new FileSigner { Name = "Testovací s.r.o.", Kind = FileSignerKind.QualifiedSeal, CertificateIssuer = "PostSignum", SigningTime = SigningTime };

        Assert.That(FileSignerDisplay.Summary(signer), Is.EqualTo("kvalifikovaná pečeť · PostSignum · bez časového razítka, uvedeno " + MomentDisplay.Text(SigningTime)));
    }

    [Test]
    public void ASummaryWithoutCertificateOrTimeSaysSo()
    {
        var signer = new FileSigner { Name = "", Kind = FileSignerKind.Other };

        Assert.That(FileSignerDisplay.Summary(signer), Is.EqualTo("elektronický podpis · bez časového razítka"));
    }

    [Test]
    public void ASignerWithoutANameIsUnknown()
    {
        var signer = new FileSigner { Name = "", Kind = FileSignerKind.Other };

        Assert.That(FileSignerDisplay.Name(signer), Is.EqualTo("Neznámý podepisující"));
    }

    [Test]
    public void TheCertificateShowsItsIssuerAndValidity()
    {
        var signer = new FileSigner
        {
            Name = "Jan Testovací",
            Kind = FileSignerKind.QualifiedSignature,
            CertificateIssuer = "I.CA",
            CertificateValidFrom = new DateTime(2026, 2, 3, 12, 0, 0, DateTimeKind.Utc),
            CertificateValidTo = new DateTime(2027, 2, 3, 12, 0, 0, DateTimeKind.Utc),
        };

        Assert.That(FileSignerDisplay.Certificate(signer), Is.EqualTo("I.CA, platný 3. 2. 2026 – 3. 2. 2027"));
    }

    [Test]
    public void NoCertificateIsADash()
    {
        var signer = new FileSigner { Name = "", Kind = FileSignerKind.Other };

        Assert.That(FileSignerDisplay.Certificate(signer), Is.EqualTo("—"));
    }
}

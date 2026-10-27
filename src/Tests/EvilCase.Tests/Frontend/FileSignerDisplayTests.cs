using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.App.Models;

namespace EvilBrains.EvilCase.Tests.Frontend;

public class FileSignerDisplayTests
{
    private static readonly DateTime Timestamped = new(2026, 10, 12, 8, 14, 0, DateTimeKind.Utc);

    private static readonly DateTime SigningTime = new(2026, 10, 12, 13, 40, 0, DateTimeKind.Utc);

    [Test]
    public void EveryProblemHasItsOwnText()
    {
        Assert.That(Enum.GetValues<FileSignersProblem>().Select(FileSignerDisplay.Problem), Is.Unique);
    }

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

    [TestCaseSource(nameof(SignerProblems))]
    public void EveryProblemHasATitle(FileSignerProblem problem)
    {
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature };

        Assert.That(FileSignerDisplay.ProblemTitle(problem, "Smlouva.pdf.p7s", signer), Is.Not.Empty);
    }

    [Test]
    public void AContentMismatchNamesTheSignedFile()
    {
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature };

        Assert.That(FileSignerDisplay.ProblemTitle(FileSignerProblem.ContentMismatch, "Smlouva o dílo.pdf.p7s", signer), Is.EqualTo("Soubor Smlouva o dílo.pdf neodpovídá podpisu."));
    }

    [Test]
    public void ASigningCertificateMismatchSaysTheSignatureNamesAnotherCertificate()
    {
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature };

        Assert.That(FileSignerDisplay.ProblemTitle(FileSignerProblem.SigningCertificateMismatch, "Smlouva.pdf.p7s", signer), Is.EqualTo("Podpis neodkazuje na certifikát podepisujícího."));
    }

    [Test]
    public void AnUnreachableRevocationNamesTheIssuer()
    {
        var signer = new FileSigner { Name = "Jan Testovací", Kind = FileSignerKind.QualifiedSignature, CertificateIssuer = "I.CA" };

        Assert.That(
            FileSignerDisplay.ProblemTitle(FileSignerProblem.RevocationUnknown, "Smlouva.pdf.p7s", signer),
            Is.EqualTo("Kontrola odvolání certifikátu u I.CA není dostupná. Podpis i řetězec certifikátů jsou v pořádku."));
    }

    [Test]
    public void AMissingSignedFileAsksForTheUpload()
    {
        Assert.That(
            FileSignerDisplay.ProblemNote(FileSignerProblem.SignedFileMissing, "Smlouva o dílo.pdf.p7s"),
            Is.EqualTo("Samostatný podpis Smlouva o dílo.pdf.p7s patří k souboru Smlouva o dílo.pdf, který mezi soubory není. Nahrajte ho pod tímto jménem."));
    }

    [Test]
    public void AMissingSignedFileOnTheCardAsksForTheUpload()
    {
        Assert.That(
            FileSignerDisplay.SignedFileProblem(DetachedSignatureProblem.Missing, "Smlouva o dílo.pdf.p7s"),
            Is.EqualTo("Soubor Smlouva o dílo.pdf mezi soubory není. Nahrajte ho pod tímto jménem."));
    }

    [Test]
    public void AnAmbiguousSignedFileOnTheCardSaysWhy()
    {
        Assert.That(
            FileSignerDisplay.SignedFileProblem(DetachedSignatureProblem.Ambiguous, "Smlouva o dílo.pdf.p7s"),
            Is.EqualTo("Soubor Smlouva o dílo.pdf nelze určit: souborů tohoto jména nebo samostatných podpisů k němu je víc."));
    }

    private static FileSignerProblem[] SignerProblems()
    {
        return Enum.GetValues<FileSignerProblem>();
    }
}

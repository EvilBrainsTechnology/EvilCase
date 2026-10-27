using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class CmsSignersTests
{
    private const string QualifiedSignaturePolicy = "0.4.0.194112.1.2";

    private static readonly DateTimeOffset ValidFrom = new(2026, 2, 3, 12, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset ValidTo = new(2027, 2, 3, 12, 0, 0, TimeSpan.Zero);

    private static readonly byte[] Content = "%PDF-1.4"u8.ToArray();

    private TestPki pki = null!;

    [SetUp]
    public void SetUpPki()
    {
        this.pki = TestPki.Create();
    }

    [TearDown]
    public void TearDownPki()
    {
        this.pki.Dispose();
    }

    [Test]
    public void EverySignerOfAnAttachedSignatureIsListed()
    {
        var person = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací", policy: QualifiedSignaturePolicy);
        var company = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Testovací stavební s.r.o.");

        var response = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [person, company]));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Problem, Is.Null);
            Assert.That(response.Signers.Select(static signer => signer.Name), Is.EquivalentTo(["Jan Testovací", "Testovací stavební s.r.o."]), "every signer the file carries is listed, in the order the file holds them");
        }
    }

    [Test]
    public void ADetachedSignatureListsItsSignersToo()
    {
        var person = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací");

        var response = CmsSigners.Read(SignedFile.Sign(Content, detached: true, [person]));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Problem, Is.Null);
            Assert.That(response.Signers.Select(static signer => signer.Name), Is.EqualTo(["Jan Testovací"]));
        }
    }

    [TestCase("0.4.0.194112.1.2", FileSignerKind.QualifiedSignature)]
    [TestCase("0.4.0.194112.1.0", FileSignerKind.QualifiedCertificateSignature)]
    [TestCase("0.4.0.194112.1.3", FileSignerKind.QualifiedSeal)]
    [TestCase("0.4.0.194112.1.1", FileSignerKind.QualifiedCertificateSeal)]
    [TestCase("1.2.3.4", FileSignerKind.Other)]
    public void TheKindFollowsTheCertificatePolicy(string policy, FileSignerKind kind)
    {
        var signer = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací", policy: policy);

        var response = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [signer]));

        Assert.That(response.Signers.Single().Kind, Is.EqualTo(kind));
    }

    [TestCase("PostSignum Test", "PostSignum")]
    [TestCase("I.CA Test", "I.CA")]
    [TestCase("eIdentity Test", "eIdentity")]
    [TestCase("Jiná", "Jiná Qualified CA")]
    public void AKnownAuthorityShowsByItsShortName(string authority, string issuer)
    {
        using var authorityPki = TestPki.Create(authority);
        var signer = authorityPki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací");

        var response = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [signer]));

        Assert.That(response.Signers.Single().CertificateIssuer, Is.EqualTo(issuer));
    }

    [Test]
    public void TheCertificateValidityComesBackInUtc()
    {
        var signer = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací");

        var read = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [signer])).Signers.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(read.CertificateValidFrom, Is.EqualTo(ValidFrom.UtcDateTime));
            Assert.That(read.CertificateValidFrom?.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(read.CertificateValidTo, Is.EqualTo(ValidTo.UtcDateTime));
            Assert.That(read.CertificateValidTo?.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }

    [Test]
    public void ATimestampTokenGivesTheTime()
    {
        var signer = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací");

        var read = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [signer], tsa: this.pki.IssueTsa())).Signers.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(read.Timestamped, Is.EqualTo(SignedFile.Timestamped.UtcDateTime));
            Assert.That(read.SigningTime, Is.EqualTo(SignedFile.SigningTime.UtcDateTime));
            Assert.That(read.SigningTime?.Kind, Is.EqualTo(DateTimeKind.Utc));
        }
    }

    [Test]
    public void WithoutATimestampOnlyTheSigningTimeComesBack()
    {
        var signer = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací");

        var read = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [signer])).Signers.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(read.Timestamped, Is.Null);
            Assert.That(read.SigningTime, Is.EqualTo(SignedFile.SigningTime.UtcDateTime));
        }
    }

    [Test]
    public void ASignerWhoseCertificateIsNotCarriedHasNoName()
    {
        var signer = this.pki.IssueSeal(ValidFrom, ValidTo, commonName: "Jan Testovací", policy: QualifiedSignaturePolicy);

        var read = CmsSigners.Read(SignedFile.Sign(Content, detached: false, [signer], includeCertificates: false)).Signers.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(read.Name, Is.Empty);
            Assert.That(read.Kind, Is.EqualTo(FileSignerKind.Other), "without the certificate nothing states a policy");
            Assert.That(read.CertificateIssuer, Is.Null);
        }
    }

    [Test]
    public void BytesThatAreNoCmsAreNotSigned()
    {
        var response = CmsSigners.Read("abc"u8);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Problem, Is.EqualTo(FileSignersProblem.NotSigned));
            Assert.That(response.Signers, Is.Empty);
        }
    }
}

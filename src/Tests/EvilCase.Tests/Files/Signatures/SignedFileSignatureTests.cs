using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ess;
using Org.BouncyCastle.Asn1.Nist;
using Org.BouncyCastle.Asn1.Oiw;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Security;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class SignedFileSignatureTests
{
    private const string QualifiedSignaturePolicy = "0.4.0.194112.1.2";

    private static readonly DateTime Now = new(2026, 10, 27, 9, 0, 0, DateTimeKind.Utc);

    private static readonly DateTimeOffset YearStart = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset YearEnd = new(2027, 1, 1, 0, 0, 0, TimeSpan.Zero);

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
    public void ATimestampedSignatureIsValidAtItsTimestamp()
    {
        var certificate = this.Signer(YearStart, new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero));
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], this.pki.IssueTsa()), revocation: revocation);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid), "a certificate expired since its timestamp still holds");
            Assert.That(signer.Problem, Is.Null);
            Assert.That(signer.RevocationChecked, Is.EqualTo(StubSignatureRevocation.CheckedAt));
            Assert.That(signer.Timestamped, Is.EqualTo(SignedFile.Timestamped.UtcDateTime));
            Assert.That(revocation.Checks, Is.EqualTo(1));
        }
    }

    [Test]
    public void ASignatureWithoutATimestampIsValidWhileItsCertificateIsValidToday()
    {
        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)]));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void AnExpiredCertificateWithoutATimestampIsUnverifiable()
    {
        var certificate = this.Signer(YearStart, new DateTimeOffset(2026, 10, 20, 0, 0, 0, TimeSpan.Zero));
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate]), revocation: revocation);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signer.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(signer.Problem, Is.EqualTo(FileSignerProblem.NotValidNow));
            Assert.That(revocation.Checks, Is.Zero);
        }
    }

    [Test]
    public void ACertificateNotYetValidAtItsTimestampIsInvalid()
    {
        var certificate = this.Signer(new DateTimeOffset(2026, 10, 13, 0, 0, 0, TimeSpan.Zero), YearEnd);

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], this.pki.IssueTsa()));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.NotValidAtTimestamp);
    }

    [Test]
    public void ATimestampOfAnotherAuthorityIsInvalid()
    {
        using var foreign = TestPki.Create("Cizí");

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], foreign.IssueTsa()));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.TimestampInvalid);
    }

    [Test]
    public void ACertificateOfAnotherAuthorityIsUnverifiable()
    {
        using var foreign = TestPki.Create("Cizí");
        var certificate = foreign.IssueSeal(YearStart, YearEnd, commonName: "Jan Testovací", policy: QualifiedSignaturePolicy);

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate]));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signer.State, Is.EqualTo(FileSignatureState.Unverifiable), "the display kind never makes a signature trusted");
            Assert.That(signer.Problem, Is.EqualTo(FileSignerProblem.UntrustedIssuer));
            Assert.That(signer.Kind, Is.EqualTo(FileSignerKind.QualifiedSignature));
            Assert.That(signer.Name, Is.EqualTo("Jan Testovací"));
        }
    }

    [Test]
    public void ACertificateWithoutAQualifiedPolicyIsUnverifiable()
    {
        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd, "1.2.3.4")]));

        AssertVerdict(signer, FileSignatureState.Unverifiable, FileSignerProblem.UntrustedIssuer);
    }

    [TestCase("0.4.0.194112.1.0")]
    [TestCase("0.4.0.194112.1.1")]
    [TestCase("0.4.0.194112.1.2")]
    [TestCase("0.4.0.194112.1.3")]
    public void EveryQualifiedPolicyIsAccepted(string policy)
    {
        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd, policy)]));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void AQualifiedCertificateUnderCasOfTheirOwnPolicyIsValid()
    {
        using var restricted = TestPki.Create("Vlastní", caPolicy: "1.2.203.1.2.3");
        var certificate = restricted.IssueSeal(YearStart, YearEnd, commonName: "Jan Testovací", policy: QualifiedSignaturePolicy);

        var response = SignedFileSignature.Verify(
            SignedFile.Sign(Content, detached: false, [certificate]),
            signedContent: null,
            restricted.Trust,
            new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked),
            Now);

        Assert.That(response.Signers.Single().State, Is.EqualTo(FileSignatureState.Valid), "the qualified policy is the certificate's own, whatever policy its CAs declare");
    }

    [Test]
    public void AnIntermediateTheFileCarriesIsNoTrustedCa()
    {
        var rootOnly = new SignatureTrust([this.pki.Root], []);
        var cms = SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], carried: [this.pki.Intermediate]);

        var response = SignedFileSignature.Verify(cms, signedContent: null, rootOnly, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked), Now);

        AssertVerdict(response.Signers.Single(), FileSignatureState.Unverifiable, FileSignerProblem.UntrustedIssuer);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ASignatureIsValidThroughTheBundledIntermediateWhileTheHostOffersAnotherOfTheSameName(bool reissuedByTheBundledRoot)
    {
        using var host = TestPki.Create("Host");
        using var hostIntermediate = HostIntermediate.Add(this.pki.ReissueIntermediate(reissuedByTheBundledRoot ? this.pki : host));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], this.pki.IssueTsa()));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ChangedContentIsInvalid()
    {
        var cms = SignedFile.Sign(Content, detached: true, [this.Signer(YearStart, YearEnd)]);

        var signer = this.VerifyOne(cms, "%PDF-1.5"u8.ToArray());

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.ContentMismatch);
    }

    [Test]
    public void ADetachedSignatureIsVerifiedAgainstTheGivenContent()
    {
        var cms = SignedFile.Sign(Content, detached: true, [this.Signer(YearStart, YearEnd)]);

        var signer = this.VerifyOne(cms, Content);

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ASignerWithoutASigningCertificateAttributeIsUnaffected()
    {
        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)]));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ASigningCertificateV2NamingTheSignerIsValid()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var attribute = SigningCertificateV2(new EssCertIDv2(SHA256.HashData(certificate.RawData), IssuerSerialOf(certificate)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], signedAttribute: attribute));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ASigningCertificateV2HashesWithItsOwnAlgorithm()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var attribute = SigningCertificateV2(new EssCertIDv2(new AlgorithmIdentifier(NistObjectIdentifiers.IdSha512), SHA512.HashData(certificate.RawData)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], signedAttribute: attribute));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ASigningCertificateV2NamingAnotherCertificateIsInvalid()
    {
        var other = this.Signer(YearStart, YearEnd);
        var attribute = SigningCertificateV2(new EssCertIDv2(SHA256.HashData(other.RawData)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], signedAttribute: attribute));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.SigningCertificateMismatch);
    }

    [Test]
    public void ASigningCertificateNamingTheSignerIsValid()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var attribute = SigningCertificate(new EssCertID(Sha1(certificate), IssuerSerialOf(certificate)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], signedAttribute: attribute));

        Assert.That(signer.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ASigningCertificateNamingAnotherCertificateIsInvalid()
    {
        var other = this.Signer(YearStart, YearEnd);
        var attribute = SigningCertificate(new EssCertID(Sha1(other)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], signedAttribute: attribute));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.SigningCertificateMismatch);
    }

    [Test]
    public void AnIssuerSerialOfAnotherCertificateIsInvalid()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var other = this.Signer(YearStart, YearEnd);
        var attribute = SigningCertificateV2(new EssCertIDv2(SHA256.HashData(certificate.RawData), IssuerSerialOf(other)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], signedAttribute: attribute));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.SigningCertificateMismatch);
    }

    [Test]
    public void AnIssuerSerialOfAnotherIssuerIsInvalid()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var serial = X509CertificateStructure.GetInstance(certificate.RawData).SerialNumber;
        var otherIssuer = new GeneralNames(new GeneralName(new X509Name("CN=Cizí Qualified CA")));
        var attribute = SigningCertificateV2(new EssCertIDv2(SHA256.HashData(certificate.RawData), new IssuerSerial(otherIssuer, serial)));

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [certificate], signedAttribute: attribute));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.SigningCertificateMismatch);
    }

    [Test]
    public void OfRenewedCertificatesTheOneTheAttributeNamesIsTheSigners()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);

        // Both certificates share the key and so the key identifier; the file is kept where it lists the older one first.
        X509Certificate2 renewed;
        byte[] cms;
        var attempts = 0;
        do
        {
            var older = this.pki.IssueSeal(YearStart, YearEnd, commonName: "Jan Testovací", policy: QualifiedSignaturePolicy, key: key);
            renewed = this.pki.IssueSeal(YearStart, YearEnd, commonName: "Jan Testovací", policy: QualifiedSignaturePolicy, key: key);
            cms = SignedFile.Sign(
                Content,
                detached: false,
                [renewed],
                includeCertificates: false,
                identifier: System.Security.Cryptography.Pkcs.SubjectIdentifierType.SubjectKeyIdentifier,
                carried: [older, renewed],
                signedAttribute: SigningCertificateV2(new EssCertIDv2(SHA256.HashData(renewed.RawData))));
        }
        while (CarriedFirst(cms).AsSpan().SequenceEqual(renewed.RawData) && ++attempts < 32);

        Assume.That(CarriedFirst(cms), Is.Not.EqualTo(renewed.RawData));

        Assert.That(this.VerifyOne(cms).State, Is.EqualTo(FileSignatureState.Valid));
    }

    [TestCase("1.2.840.113549.1.9.16.2.47")]
    [TestCase("1.2.840.113549.1.9.16.2.12")]
    public void AMalformedSigningCertificateAttributeIsInvalid(string oid)
    {
        var attribute = new AsnEncodedData(new Oid(oid), new DerOctetString([1, 2, 3]).GetEncoded());

        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], signedAttribute: attribute));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.SigningCertificateMismatch);
    }

    [Test]
    public void ARevokedCertificateIsInvalid()
    {
        var signer = this.VerifyOne(
            SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)]),
            revocation: new StubSignatureRevocation(SignatureRevocationStatus.Revoked));

        AssertVerdict(signer, FileSignatureState.Invalid, FileSignerProblem.Revoked);
    }

    [Test]
    public void AnUnknownRevocationIsUnverifiable()
    {
        var signer = this.VerifyOne(
            SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)]),
            revocation: new StubSignatureRevocation(SignatureRevocationStatus.Unknown));

        using (Assert.EnterMultipleScope())
        {
            AssertVerdict(signer, FileSignatureState.Unverifiable, FileSignerProblem.RevocationUnknown);
            Assert.That(signer.RevocationChecked, Is.Null);
        }
    }

    [Test]
    public void ASignerWithoutItsCertificateIsUnverifiable()
    {
        var signer = this.VerifyOne(SignedFile.Sign(Content, detached: false, [this.Signer(YearStart, YearEnd)], includeCertificates: false));

        using (Assert.EnterMultipleScope())
        {
            AssertVerdict(signer, FileSignatureState.Unverifiable, FileSignerProblem.CertificateMissing);
            Assert.That(signer.Name, Is.Empty);
        }
    }

    [Test]
    public void EverySignerGetsItsOwnVerdict()
    {
        using var foreign = TestPki.Create("Cizí");
        var trusted = this.Signer(YearStart, YearEnd);
        var untrusted = foreign.IssueSeal(YearStart, YearEnd, commonName: "Petra Testovací", policy: QualifiedSignaturePolicy);

        var response = this.Verify(SignedFile.Sign(Content, detached: false, [trusted, untrusted]));

        // DER sorts the signer infos by their encoding, which the signature's length changes.
        Assert.That(
            response.Signers.Select(static signer => (signer.Name, signer.State)),
            Is.EquivalentTo(new (string?, FileSignatureState?)[] { ("Jan Testovací", FileSignatureState.Valid), ("Petra Testovací", FileSignatureState.Unverifiable) }));
    }

    [Test]
    public void SignersOfOneCertificateAskForItsRevocationOnce()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var response = this.Verify(SignedFile.Sign(Content, detached: false, [certificate, certificate]), revocation: revocation);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Signers.Select(static signer => signer.State), Is.All.EqualTo(FileSignatureState.Valid));
            Assert.That(revocation.Checks, Is.EqualTo(1));
        }
    }

    [Test]
    public void BytesThatAreNoCmsAreNotSigned()
    {
        var response = this.Verify("no cms"u8.ToArray());

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Problem, Is.EqualTo(FileSignersProblem.NotSigned));
            Assert.That(response.Signers, Is.Empty);
        }
    }

    [Test]
    public void MoreThanFiftySignersAreTooMany()
    {
        var certificate = this.Signer(YearStart, YearEnd);
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var response = this.Verify(SignedFile.Sign(Content, detached: false, [.. Enumerable.Repeat(certificate, FileLimits.MaxSigners + 1)]), revocation: revocation);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Problem, Is.EqualTo(FileSignersProblem.TooManySigners));
            Assert.That(revocation.Checks, Is.Zero);
        }
    }

    [Test]
    public void UnverifiableMarksEverySigner()
    {
        var listed = CmsSigners.Read(SignedFile.Sign(Content, detached: true, [this.Signer(YearStart, YearEnd), this.Signer(YearStart, YearEnd)]));

        var response = SignedFileSignature.Unverifiable(listed, FileSignerProblem.SignedFileMissing);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Signers.Select(static signer => signer.State), Is.All.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(response.Signers.Select(static signer => signer.Problem), Is.All.EqualTo(FileSignerProblem.SignedFileMissing));
            Assert.That(response.Signers, Has.Count.EqualTo(2));
        }
    }

    private static void AssertVerdict(FileSigner signer, FileSignatureState state, FileSignerProblem problem)
    {
        Assert.That((signer.State, signer.Problem), Is.EqualTo(((FileSignatureState?)state, (FileSignerProblem?)problem)));
    }

    private static AsnEncodedData SigningCertificateV2(EssCertIDv2 named)
    {
        return new AsnEncodedData(new Oid(Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.IdAASigningCertificateV2.Id), new SigningCertificateV2(named).GetEncoded());
    }

    private static AsnEncodedData SigningCertificate(EssCertID named)
    {
        return new AsnEncodedData(new Oid(Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.IdAASigningCertificate.Id), new SigningCertificate(named).GetEncoded());
    }

    private static byte[] CarriedFirst(byte[] cms)
    {
        var signed = new System.Security.Cryptography.Pkcs.SignedCms();
        signed.Decode(cms);

        return signed.Certificates[0].RawData;
    }

    private static byte[] Sha1(X509Certificate2 certificate)
    {
        return DigestUtilities.CalculateDigest(OiwObjectIdentifiers.IdSha1, certificate.RawData);
    }

    private static IssuerSerial IssuerSerialOf(X509Certificate2 certificate)
    {
        var structure = X509CertificateStructure.GetInstance(certificate.RawData);

        return new IssuerSerial(new GeneralNames(new GeneralName(structure.Issuer)), structure.SerialNumber);
    }

    private X509Certificate2 Signer(DateTimeOffset notBefore, DateTimeOffset notAfter, string policy = QualifiedSignaturePolicy)
    {
        return this.pki.IssueSeal(notBefore, notAfter, commonName: "Jan Testovací", policy: policy);
    }

    private FileSigner VerifyOne(byte[] cms, byte[]? signedContent = null, StubSignatureRevocation? revocation = null)
    {
        return this.Verify(cms, signedContent, revocation).Signers.Single();
    }

    private FileSignersResponse Verify(byte[] cms, byte[]? signedContent = null, StubSignatureRevocation? revocation = null)
    {
        return SignedFileSignature.Verify(cms, signedContent, this.pki.Trust, revocation ?? new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked), Now);
    }
}

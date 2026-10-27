using System.Text;
using EvilBrains.EvilCase.Api.Contract.Files;
using EvilBrains.EvilCase.Business.Files.Signatures;
using EvilBrains.EvilCase.Tests.Frontend;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class DataMessageSignatureTests
{
    private static readonly DateTimeOffset SealFrom = new(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset SealTo = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset Timestamped = new(2025, 6, 1, 12, 0, 0, TimeSpan.Zero);

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
    public void ASealedMessageIsValidAtItsTimestamp()
    {
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var signature = DataMessageSignature.Verify(this.SealedMessage(), this.pki.Trust, revocation);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signature.State, Is.EqualTo(FileSignatureState.Valid));
            Assert.That(signature.Problem, Is.Null);
            Assert.That(signature.CertificateIssuer, Is.EqualTo("Test Qualified CA"));
            Assert.That(signature.CertificateValidFrom, Is.EqualTo(SealFrom.UtcDateTime));
            Assert.That(signature.CertificateValidTo, Is.EqualTo(SealTo.UtcDateTime));
            Assert.That(signature.Timestamped, Is.EqualTo(Timestamped.UtcDateTime), "the seal is judged at the timestamp over the signature");
            Assert.That(signature.Timestamped?.Kind, Is.EqualTo(DateTimeKind.Utc));
            Assert.That(signature.RevocationChecked, Is.EqualTo(StubSignatureRevocation.CheckedAt));
            Assert.That(revocation.Checks, Is.EqualTo(1));
        }
    }

    [Test]
    public void ASealedReceiptIsValid()
    {
        var zfo = this.Sealed(SyntheticZfo.ReceiptXml(SyntheticZfo.Namespace));

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        Assert.That(signature.State, Is.EqualTo(FileSignatureState.Valid));
    }

    [Test]
    public void ASealThatExpiredAfterItsTimestampStaysValid()
    {
        var seal = this.pki.IssueSeal(SealFrom, new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero));
        var zfo = SealedZfo.Seal(MessageXml(), seal, this.pki.IssueTsa(), Timestamped);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        Assert.That(signature.State, Is.EqualTo(FileSignatureState.Valid), "a seal valid at its timestamp stays valid after it expires");
    }

    [Test]
    public void AlteredContentIsAMismatch()
    {
        var zfo = this.SealedMessage();
        var subject = Encoding.UTF8.GetBytes("Rozhodnutí o přestupku");
        var at = zfo.AsSpan().IndexOf(subject);
        zfo[at] = (byte)'X';

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.ContentMismatch);
    }

    [Test]
    public void ASealFromAForeignHierarchyIsNotPostSignumAndIsNeverCheckedForRevocation()
    {
        using var foreign = TestPki.Create("Foreign");
        var zfo = SealedZfo.Seal(MessageXml(), foreign.IssueSeal(SealFrom, SealTo), this.pki.IssueTsa(), Timestamped);
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, revocation);

        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(signature, FileSignatureProblem.NotPostSignum);
            Assert.That(revocation.Checks, Is.Zero, "only a certificate that chained to the bundled roots is checked online");
        }
    }

    [Test]
    public void ASealNotYetValidAtItsTimestampIsInvalid()
    {
        var seal = this.pki.IssueSeal(Timestamped.AddDays(1), SealTo);
        var zfo = SealedZfo.Seal(MessageXml(), seal, this.pki.IssueTsa(), Timestamped);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.NotValidAtTimestamp);
    }

    [Test]
    public void ASealExpiredBeforeItsTimestampIsInvalid()
    {
        var seal = this.pki.IssueSeal(SealFrom, Timestamped.AddDays(-1));
        var zfo = SealedZfo.Seal(MessageXml(), seal, this.pki.IssueTsa(), Timestamped);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.NotValidAtTimestamp);
    }

    [Test]
    public void ASealOfAnotherOrganizationIsNotTheIsds()
    {
        var seal = this.pki.IssueSeal(SealFrom, SealTo, organizationIdentifier: "NTRCZ-00000001");
        var zfo = SealedZfo.Seal(MessageXml(), seal, this.pki.IssueTsa(), Timestamped);
        var revocation = new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, revocation);

        using (Assert.EnterMultipleScope())
        {
            AssertInvalid(signature, FileSignatureProblem.NotIsdsSigner);
            Assert.That(revocation.Checks, Is.Zero);
        }
    }

    [Test]
    public void ARevokedSealIsInvalid()
    {
        var signature = DataMessageSignature.Verify(this.SealedMessage(), this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.Revoked));

        AssertInvalid(signature, FileSignatureProblem.Revoked);
    }

    [Test]
    public void AnUnknownRevocationStatusIsUnverifiable()
    {
        var signature = DataMessageSignature.Verify(this.SealedMessage(), this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.Unknown));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(signature.State, Is.EqualTo(FileSignatureState.Unverifiable));
            Assert.That(signature.Problem, Is.Null);
            Assert.That(signature.Timestamped, Is.EqualTo(Timestamped.UtcDateTime));
            Assert.That(signature.RevocationChecked, Is.Null, "an unknown status was never obtained");
        }
    }

    [Test]
    public void ASealWithoutATimestampIsInvalid()
    {
        var zfo = SealedZfo.Seal(MessageXml(), this.pki.IssueSeal(SealFrom, SealTo), tsa: null, Timestamped);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.TimestampInvalid);
    }

    [Test]
    public void ATimestampOverOtherDataIsInvalid()
    {
        var zfo = SealedZfo.Seal(MessageXml(), this.pki.IssueSeal(SealFrom, SealTo), this.pki.IssueTsa(), Timestamped, timestampedData: [1, 2, 3]);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.TimestampInvalid);
    }

    [Test]
    public void ATimestampFromAForeignTsaIsInvalid()
    {
        using var foreign = TestPki.Create("Foreign");
        var zfo = SealedZfo.Seal(MessageXml(), this.pki.IssueSeal(SealFrom, SealTo), foreign.IssueTsa(), Timestamped);

        var signature = DataMessageSignature.Verify(zfo, this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.TimestampInvalid);
    }

    [Test]
    public void BytesThatAreNoCmsAreNoDataMessage()
    {
        var signature = DataMessageSignature.Verify(Encoding.UTF8.GetBytes("%PDF-1.7"), this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.NotDataMessage);
    }

    [Test]
    public void ASealedFormIsNoDataMessage()
    {
        var signature = DataMessageSignature.Verify(this.Sealed("<form/>"), this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.NotDataMessage);
    }

    [Test]
    public void ContentWithADocumentTypeIsNoDataMessage()
    {
        const string xml = "<!DOCTYPE q [<!ENTITY e \"x\">]><q:dmReturnedMessage xmlns:q=\"urn:test\">&e;</q:dmReturnedMessage>";

        var signature = DataMessageSignature.Verify(this.Sealed(xml), this.pki.Trust, new StubSignatureRevocation(SignatureRevocationStatus.NotRevoked));

        AssertInvalid(signature, FileSignatureProblem.NotDataMessage);
    }

    private static string MessageXml()
    {
        return SyntheticZfo.MessageXml(SyntheticZfo.Namespace, [("Rozhodnutí.pdf", [1, 2, 3])]);
    }

    private static void AssertInvalid(FileSignatureResponse signature, FileSignatureProblem problem)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(signature.State, Is.EqualTo(FileSignatureState.Invalid));
            Assert.That(signature.Problem, Is.EqualTo(problem));
            Assert.That(signature.RevocationChecked, Is.Null);
        }
    }

    private byte[] SealedMessage()
    {
        return this.Sealed(MessageXml());
    }

    private byte[] Sealed(string xml)
    {
        return SealedZfo.Seal(xml, this.pki.IssueSeal(SealFrom, SealTo), this.pki.IssueTsa(), Timestamped);
    }
}

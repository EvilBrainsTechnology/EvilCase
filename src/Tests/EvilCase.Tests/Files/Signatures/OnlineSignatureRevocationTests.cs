using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Business.Files.Signatures;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Frontend;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

public class OnlineSignatureRevocationTests
{
    [Test]
    public void ACertificateWhoseIssuerNamesNoRevocationSourceHasAnUnknownStatus()
    {
        using var pki = TestPki.Create();
        var seal = pki.IssueSeal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        var now = new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc);
        var logger = new CapturingLogger<OnlineSignatureRevocation>();
        var revocation = new OnlineSignatureRevocation(new TestTimeProvider(now), logger);

        var result = revocation.CheckRevocation(seal, pki.Trust);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(SignatureRevocationStatus.Unknown), "a status nobody answered for is unknown, never good");
            Assert.That(result.Checked, Is.EqualTo(now));
            Assert.That(logger.LoggedWarningOrAbove, Is.True, "an unknown status is logged as a warning");
        }
    }

    [Test]
    public void AStatusReadThroughTheBundledIntermediateIsNotRevoked()
    {
        using var crls = new CrlServer();
        using var pki = PublishedPki(crls);
        var seal = pki.IssueSeal(Now.AddYears(-1), Now.AddYears(1));
        var revocation = new OnlineSignatureRevocation(new TestTimeProvider(Now.UtcDateTime), new CapturingLogger<OnlineSignatureRevocation>());

        var result = revocation.CheckRevocation(seal, pki.Trust);

        Assert.That(result.Status, Is.EqualTo(SignatureRevocationStatus.NotRevoked));
    }

    [Test]
    public void AStatusReadThroughAHostIntermediateIsUnknown()
    {
        using var crls = new CrlServer();
        using var pki = PublishedPki(crls);
        var seal = pki.IssueSeal(Now.AddYears(-1), Now.AddYears(1));
        using var hostIntermediate = HostIntermediate.Add(pki.ReissueIntermediate(pki));
        Assume.That(BuildsOutsideTheBundle(pki.Trust, seal), Is.True, "the platform prefers the host's intermediate");
        var logger = new CapturingLogger<OnlineSignatureRevocation>();
        var revocation = new OnlineSignatureRevocation(new TestTimeProvider(Now.UtcDateTime), logger);

        var result = revocation.CheckRevocation(seal, pki.Trust);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Status, Is.EqualTo(SignatureRevocationStatus.Unknown), "a status read outside the bundled hierarchy is not its status");
            Assert.That(logger.LoggedWarningOrAbove, Is.True);
        }
    }

    private static readonly DateTimeOffset Now = new(DateTime.UtcNow.Date, TimeSpan.Zero);

    /// <summary>
    /// A PKI of its own name, so no CRL the platform cached for another test applies, with its CRLs published.
    /// </summary>
    private static TestPki PublishedPki(CrlServer crls)
    {
        var pki = TestPki.Create($"Revocation {Guid.NewGuid():N}", crlBase: crls.BaseUri);
        crls.Publish(TestPki.RootCrlName, pki.RootCrl(Now));
        crls.Publish(TestPki.IntermediateCrlName, pki.IntermediateCrl(Now));

        return pki;
    }

    private static bool BuildsOutsideTheBundle(SignatureTrust trust, X509Certificate2 certificate)
    {
        using var chain = trust.NewChain();
        chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;

        return chain.Build(certificate) && !trust.IsWithinBundle(chain);
    }
}

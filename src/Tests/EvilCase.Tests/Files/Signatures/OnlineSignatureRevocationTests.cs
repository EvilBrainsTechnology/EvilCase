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
    public void ARevocationListThatCannotBeFetchedLeavesTheStatusUnknown()
    {
        using var pki = TestPki.Create();
        var seal = pki.IssueSeal(
            new DateTimeOffset(2021, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2022, 1, 1, 0, 0, 0, TimeSpan.Zero),
            revocationListUrl: "http://127.0.0.1:9/retired.crl");
        var revocation = new OnlineSignatureRevocation(new TestTimeProvider(new DateTime(2026, 10, 27, 8, 0, 0, DateTimeKind.Utc)), new CapturingLogger<OnlineSignatureRevocation>());

        var result = revocation.CheckRevocation(seal, pki.Trust);

        Assert.That(result.Status, Is.EqualTo(SignatureRevocationStatus.Unknown), "a retired hierarchy whose list no longer answers is unverifiable, never revoked");
    }
}

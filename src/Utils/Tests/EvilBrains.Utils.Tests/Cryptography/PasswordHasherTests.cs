using EvilBrains.Cryptography;

namespace EvilBrains.Utils.Tests.Cryptography;

public class PasswordHasherTests
{
    private const string Password = "correct-horse-battery-staple";

    [Test]
    public void APasswordVerifiesAgainstItsOwnHash()
    {
        Assert.That(PasswordHasher.Verify(Password, PasswordHasher.Hash(Password)), Is.True);
    }

    [Test]
    public void AnotherPasswordDoesNot()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(PasswordHasher.Verify("not-the-password", PasswordHasher.Hash(Password)), Is.False);
            Assert.That(PasswordHasher.Verify(Password.ToUpperInvariant(), PasswordHasher.Hash(Password)), Is.False);
            Assert.That(PasswordHasher.Verify("", PasswordHasher.Hash(Password)), Is.False);
        }
    }

    [Test]
    public void TheSamePasswordHashesDifferentlyEveryTime()
    {
        var first = PasswordHasher.Hash(Password);
        var second = PasswordHasher.Hash(Password);

        Assert.That(second, Is.Not.EqualTo(first));
    }

    [Test]
    public void TheHashCarriesItsOwnParameters()
    {
        var segments = PasswordHasher.Hash(Password).Split(':');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(segments, Has.Length.EqualTo(4), "raising the parameters later must not invalidate stored hashes");
            Assert.That(int.Parse(segments[2], CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(600_000), "raising the parameters later must not invalidate stored hashes");
            Assert.That(segments[3], Is.EqualTo("SHA256"), "raising the parameters later must not invalidate stored hashes");
        }
    }

    [Test]
    public void TheDecoyVerificationDoesNotThrow()
    {
        Assert.DoesNotThrow(PasswordHasher.FakeVerify, "the decoy spends a real verification's time and must not throw");
    }

    [Test]
    public void TheHashFitsTheColumnItIsStoredIn()
    {
        Assert.That(PasswordHasher.Hash(Password), Has.Length.LessThanOrEqualTo(256), "the hash is stored in a 256-character column");
    }
}

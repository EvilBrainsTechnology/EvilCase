using EvilBrains.Cryptography;

namespace EvilBrains.Utils.Tests.Cryptography;

/// <summary>
/// The one thing standing between a leaked database and everybody's account.
/// </summary>
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

    /// <summary>
    /// The salt is per hash, so two accounts sharing a password must not share a row anyone could spot.
    /// </summary>
    [Test]
    public void TheSamePasswordHashesDifferentlyEveryTime()
    {
        var first = PasswordHasher.Hash(Password);
        var second = PasswordHasher.Hash(Password);

        Assert.That(second, Is.Not.EqualTo(first));
    }

    /// <summary>
    /// The parameters travel with the hash, so raising them later must not invalidate what is stored.
    /// </summary>
    [Test]
    public void TheHashCarriesItsOwnParameters()
    {
        var segments = PasswordHasher.Hash(Password).Split(':');

        using (Assert.EnterMultipleScope())
        {
            Assert.That(segments, Has.Length.EqualTo(4));
            Assert.That(int.Parse(segments[2], CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(600_000));
            Assert.That(segments[3], Is.EqualTo("SHA256"));
        }
    }

    /// <summary>
    /// Runs on the sign-in path for an e-mail nobody has, purely to spend the same time a real
    /// verification would. It must not be the thing that throws instead.
    /// </summary>
    [Test]
    public void TheDecoyVerificationDoesNotThrow()
    {
        Assert.DoesNotThrow(PasswordHasher.FakeVerify);
    }

    /// <summary>
    /// The column it is stored in is not unbounded.
    /// </summary>
    [Test]
    public void TheHashFitsTheColumnItIsStoredIn()
    {
        Assert.That(PasswordHasher.Hash(Password), Has.Length.LessThanOrEqualTo(256));
    }
}

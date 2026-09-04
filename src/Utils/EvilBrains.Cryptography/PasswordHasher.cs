using System.Security.Cryptography;

namespace EvilBrains.Cryptography;

public static class PasswordHasher
{
    private const int SaltSize = 16;

    private const int KeySize = 32;

    // OWASP recommendation for PBKDF2-HMAC-SHA256
    private const int Iterations = 600_000;

    private const char SegmentDelimiter = ':';

    private static readonly HashAlgorithmName HashAlgorithm = HashAlgorithmName.SHA256;

    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithm, KeySize);
        return string.Join(SegmentDelimiter, Convert.ToHexString(hash), Convert.ToHexString(salt), Iterations, HashAlgorithm);
    }

    public static bool Verify(string password, string passwordHash)
    {
        var segments = passwordHash.Split(SegmentDelimiter);
        var hash = Convert.FromHexString(segments[0]);
        var salt = Convert.FromHexString(segments[1]);
        var iterations = int.Parse(segments[2], CultureInfo.InvariantCulture);
        var hashAlgorithm = new HashAlgorithmName(segments[3]);

        var inputHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, hashAlgorithm, hash.Length);
        return CryptographicOperations.FixedTimeEquals(inputHash, hash);
    }

    public static void FakeVerify()
    {
        const string fakePassword = "";
        var fakeHash = new string('0', KeySize);
        var fakeSalt = new string('0', KeySize);
        var fakeFinalHash = string.Join(SegmentDelimiter, fakeHash, fakeSalt, Iterations, HashAlgorithm);

        Verify(fakePassword, fakeFinalHash);
    }
}

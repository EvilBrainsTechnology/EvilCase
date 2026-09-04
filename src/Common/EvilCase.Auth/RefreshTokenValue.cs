using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// SHA-256, not a KDF: 256 random bits cannot be guessed, but a dump must not hand out sessions.
/// </summary>
internal static class RefreshTokenValue
{
    private const int SizeInBytes = 32;

    public static string Create()
    {
        return Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}

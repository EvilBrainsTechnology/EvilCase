using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// The refresh token itself: 256 bits of randomness, stored only as a hash. A password KDF would be
/// wrong here — there is nothing to guess — but a database dump still must not hand out live sessions.
/// </summary>
internal static class RefreshTokenValue
{
    private const int SizeInBytes = 32;

    public static string Create() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));

    public static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}

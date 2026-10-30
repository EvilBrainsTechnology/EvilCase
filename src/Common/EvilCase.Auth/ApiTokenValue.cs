using System.Buffers.Text;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// SHA-256, not a KDF, for the same reason as <see cref="RefreshTokenValue"/>. The prefix picks the
/// authentication scheme and lets a secret scanner recognise a leaked token.
/// </summary>
internal static class ApiTokenValue
{
    public const string Prefix = "evc_";

    private const int SizeInBytes = 32;

    public static string Create()
    {
        return Prefix + Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(SizeInBytes));
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    /// <summary>
    /// The API token a single <c>Authorization: Bearer</c> header carries; any other header is the JWT
    /// scheme's to judge.
    /// </summary>
    public static string? Read(HttpRequest request)
    {
        var headers = request.Headers.Authorization;

        if (headers.Count != 1 || !AuthenticationHeaderValue.TryParse(headers[0], out var header))
            return null;

        return string.Equals(header.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase)
            && header.Parameter is { } value
            && value.StartsWith(Prefix, StringComparison.Ordinal)
                ? value
                : null;
    }
}

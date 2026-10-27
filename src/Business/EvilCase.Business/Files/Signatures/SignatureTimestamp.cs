using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// The RFC 3161 timestamp token a signer carries as an unsigned attribute.
/// </summary>
internal static class SignatureTimestamp
{
    private const string TimestampTokenOid = "1.2.840.113549.1.9.16.2.14";

    private const string TimeStampingOid = "1.3.6.1.5.5.7.3.8";

    /// <summary>
    /// Whether the signer carries a timestamp token, valid or not.
    /// </summary>
    public static bool IsPresent(SignerInfo signer)
    {
        return Token(signer) is not null;
    }

    /// <summary>
    /// The time the token states, trusted or not; null without a decodable token.
    /// </summary>
    public static DateTime? StatedTime(SignerInfo signer)
    {
        return Token(signer) is { } attribute && Rfc3161TimestampToken.TryDecode(attribute.Values[0].RawData, out var token, out _)
            ? token.TokenInfo.Timestamp.UtcDateTime
            : null;
    }

    /// <summary>
    /// The time of the timestamp token over the signature, or null where the token is missing, does not cover this signature,
    /// or its TSA does not chain to the trust's roots at that time through bundled certificates only.
    /// </summary>
    public static DateTime? TrustedTime(SignerInfo signer, SignatureTrust trust)
    {
        if (Token(signer) is not { } attribute)
            return null;

        if (!Rfc3161TimestampToken.TryDecode(attribute.Values[0].RawData, out var token, out _))
            return null;

        if (!token.VerifySignatureForSignerInfo(signer, out var tsaCertificate, trust.Intermediates) || tsaCertificate is null)
            return null;

        var timestamped = token.TokenInfo.Timestamp.UtcDateTime;

        return trust.Chains(tsaCertificate, timestamped, applicationPolicy: TimeStampingOid) ? timestamped : null;
    }

    private static CryptographicAttributeObject? Token(SignerInfo signer)
    {
        var attribute = signer.UnsignedAttributes
            .Cast<CryptographicAttributeObject>()
            .FirstOrDefault(static attribute => string.Equals(attribute.Oid.Value, TimestampTokenOid, StringComparison.Ordinal));

        return attribute is { Values.Count: > 0 } ? attribute : null;
    }
}

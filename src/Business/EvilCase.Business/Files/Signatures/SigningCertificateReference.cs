using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Ess;
using Org.BouncyCastle.Asn1.Oiw;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Security;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// The ESS signing-certificate attributes (RFC 2634, RFC 5035) a signer signs to name its certificate.
/// </summary>
internal static class SigningCertificateReference
{
    private static readonly string SigningCertificateV2Oid = Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.IdAASigningCertificateV2.Id;

    private static readonly string SigningCertificateOid = Org.BouncyCastle.Asn1.Pkcs.PkcsObjectIdentifiers.IdAASigningCertificate.Id;

    /// <summary>
    /// Whether every signing-certificate attribute of <paramref name="info"/> names <paramref name="certificate"/> first;
    /// a signer without one refers to any certificate.
    /// </summary>
    public static bool RefersTo(SignerInfo info, X509Certificate2 certificate)
    {
        foreach (var attribute in info.SignedAttributes)
        {
            var oid = attribute.Oid.Value;
            var v2 = string.Equals(oid, SigningCertificateV2Oid, StringComparison.Ordinal);
            if (!v2 && !string.Equals(oid, SigningCertificateOid, StringComparison.Ordinal))
                continue;

            if (attribute.Values.Count == 0)
                return false;

            foreach (var value in attribute.Values)
            {
                if (!Names(value.RawData, v2, certificate))
                    return false;
            }
        }

        return true;
    }

    private static bool Names(byte[] attribute, bool v2, X509Certificate2 certificate)
    {
        try
        {
            if (v2)
            {
                var first = SigningCertificateV2.GetInstance(Asn1Object.FromByteArray(attribute)).GetCerts().FirstOrDefault();

                return first is not null && Matches(first.HashAlgorithm.Algorithm, first.GetCertHash(), first.IssuerSerial, certificate);
            }

            var firstV1 = SigningCertificate.GetInstance(Asn1Object.FromByteArray(attribute)).GetCerts().FirstOrDefault();

            // RFC 2634 fixes SHA-1 for the signing-certificate attribute.
            return firstV1 is not null && Matches(OiwObjectIdentifiers.IdSha1, firstV1.GetCertHash(), firstV1.IssuerSerial, certificate);
        }
        catch (Exception exception) when (exception is not OutOfMemoryException)
        {
            // BouncyCastle reports malformed input and unknown digests through assorted exception types.
            return false;
        }
    }

    private static bool Matches(DerObjectIdentifier hashAlgorithm, byte[] hash, IssuerSerial? issuerSerial, X509Certificate2 certificate)
    {
        return DigestUtilities.CalculateDigest(hashAlgorithm, certificate.RawData).AsSpan().SequenceEqual(hash)
            && IssuerSerialMatches(issuerSerial, certificate);
    }

    private static bool IssuerSerialMatches(IssuerSerial? issuerSerial, X509Certificate2 certificate)
    {
        if (issuerSerial is null)
            return true;

        var structure = X509CertificateStructure.GetInstance(certificate.RawData);

        return issuerSerial.Serial.Value.Equals(structure.SerialNumber.Value)
            && issuerSerial.Issuer.GetNames().Any(name => name.TagNo == GeneralName.DirectoryName && X509Name.GetInstance(name.Name).Equivalent(structure.Issuer, inOrder: true));
    }
}

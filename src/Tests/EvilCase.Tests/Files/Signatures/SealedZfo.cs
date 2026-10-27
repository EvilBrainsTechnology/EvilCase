using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// A ZFO sealed the way ISDS seals one: a CMS SignedData over the XML with the seal certificate only, and an RFC 3161
/// timestamp token over the signature as an unsigned attribute.
/// </summary>
internal static class SealedZfo
{
    private const string TimestampTokenOid = "1.2.840.113549.1.9.16.2.14";

    private const string TstInfoOid = "1.2.840.113549.1.9.16.1.4";

    private const string SigningCertificateV2Oid = "1.2.840.113549.1.9.16.2.47";

    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";

    /// <summary>
    /// The CMS sealed by <paramref name="seal"/>; a timestamp token over the signature where <paramref name="tsa"/> is
    /// given, over <paramref name="timestampedData"/> instead where that is given.
    /// </summary>
    public static byte[] Seal(string xml, X509Certificate2 seal, X509Certificate2? tsa, DateTimeOffset timestamped, byte[]? timestampedData = null)
    {
        var cms = new SignedCms(new ContentInfo(Encoding.UTF8.GetBytes(xml)));
        var signer = new CmsSigner(seal) { IncludeOption = X509IncludeOption.EndCertOnly, DigestAlgorithm = new Oid(Sha256Oid) };
        signer.SignedAttributes.Add(new Pkcs9SigningTime(timestamped.UtcDateTime));
        cms.ComputeSignature(signer);

        if (tsa is not null)
        {
            var signerInfo = cms.SignerInfos[0];
            var token = TimestampToken(tsa, timestampedData ?? signerInfo.GetSignature(), timestamped);

            signerInfo.AddUnsignedAttribute(new AsnEncodedData(new Oid(TimestampTokenOid), token));
        }

        return cms.Encode();
    }

    private static byte[] TimestampToken(X509Certificate2 tsa, byte[] data, DateTimeOffset timestamped)
    {
        var info = new Rfc3161TimestampTokenInfo(
            new Oid("1.2.3.4"),
            new Oid(Sha256Oid),
            SHA256.HashData(data),
            new byte[] { 0x01, 0x02 },
            timestamped);

        var cms = new SignedCms(new ContentInfo(new Oid(TstInfoOid), info.Encode()));
        var signer = new CmsSigner(tsa) { IncludeOption = X509IncludeOption.EndCertOnly, DigestAlgorithm = new Oid(Sha256Oid) };
        signer.SignedAttributes.Add(SigningCertificateV2(tsa));
        cms.ComputeSignature(signer);

        return cms.Encode();
    }

    // SigningCertificateV2 ::= SEQUENCE { certs SEQUENCE OF ESSCertIDv2 }, ESSCertIDv2 ::= SEQUENCE { certHash OCTET STRING }
    // with SHA-256, the default hash algorithm, left out.
    private static AsnEncodedData SigningCertificateV2(X509Certificate2 certificate)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);

        using (writer.PushSequence())
        using (writer.PushSequence())
        using (writer.PushSequence())
            writer.WriteOctetString(SHA256.HashData(certificate.RawData));

        return new AsnEncodedData(new Oid(SigningCertificateV2Oid), writer.Encode());
    }
}

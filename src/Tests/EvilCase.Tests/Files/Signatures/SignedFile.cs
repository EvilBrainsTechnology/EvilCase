using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// A signed file (p7m, p7s): a CMS SignedData by one or more signers, each stating its signing time.
/// </summary>
internal static class SignedFile
{
    public static readonly DateTimeOffset SigningTime = new(2026, 10, 12, 13, 40, 0, TimeSpan.Zero);

    public static readonly DateTimeOffset Timestamped = new(2026, 10, 12, 8, 14, 0, TimeSpan.Zero);

    private const string Sha256Oid = "2.16.840.1.101.3.4.2.1";

    private const string TimestampTokenOid = "1.2.840.113549.1.9.16.2.14";

    /// <summary>
    /// The CMS over <paramref name="content"/> by <paramref name="signers"/> in order; a timestamp token over the first
    /// signature where <paramref name="tsa"/> is given.
    /// </summary>
    public static byte[] Sign(
        byte[] content,
        bool detached,
        IReadOnlyList<X509Certificate2> signers,
        X509Certificate2? tsa = null,
        bool includeCertificates = true,
        SubjectIdentifierType identifier = SubjectIdentifierType.IssuerAndSerialNumber)
    {
        var cms = new SignedCms(new ContentInfo(content), detached);

        foreach (var certificate in signers)
        {
            var signer = new CmsSigner(identifier, certificate)
            {
                IncludeOption = includeCertificates ? X509IncludeOption.EndCertOnly : X509IncludeOption.None,
                DigestAlgorithm = new Oid(Sha256Oid),
            };
            signer.SignedAttributes.Add(new Pkcs9SigningTime(SigningTime.UtcDateTime));
            cms.ComputeSignature(signer);
        }

        if (tsa is not null)
        {
            var first = cms.SignerInfos[0];

            first.AddUnsignedAttribute(new AsnEncodedData(new Oid(TimestampTokenOid), SealedZfo.TimestampToken(tsa, first.GetSignature(), Timestamped)));
        }

        return cms.Encode();
    }

    /// <summary>
    /// <paramref name="signed"/> with the carried <paramref name="certificate"/> overwritten by a DER sequence of the
    /// same length that is no certificate.
    /// </summary>
    public static byte[] WithCertificateReplaced(byte[] signed, byte[] certificate)
    {
        var at = signed.AsSpan().IndexOf(certificate);
        var replaced = signed.ToArray();
        var length = certificate.Length;
        byte[] noCertificate = [0x30, 0x82, (byte)((length - 4) >> 8), (byte)(length - 4), 0x04, 0x82, (byte)((length - 8) >> 8), (byte)(length - 8)];

        noCertificate.CopyTo(replaced, at);
        replaced.AsSpan(at + noCertificate.Length, length - noCertificate.Length).Clear();

        return replaced;
    }

    /// <summary>
    /// The CMS over <paramref name="content"/> by a throwaway self-signed signer.
    /// </summary>
    public static byte[] Sign(byte[] content, bool detached)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Testovací podepisující", key, HashAlgorithmName.SHA256);
        using var certificate = request.CreateSelfSigned(
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            new DateTimeOffset(2027, 1, 1, 0, 0, 0, TimeSpan.Zero));

        return Sign(content, detached, [certificate]);
    }
}

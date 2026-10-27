using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Lists the signers of a CMS SignedData, attached or detached, as the file states them. Nothing is verified.
/// </summary>
internal static class CmsSigners
{
    private const string CertificatePoliciesOid = "2.5.29.32";

    /// <summary>
    /// ETSI QCP-n: a natural person's qualified certificate.
    /// </summary>
    private const string QualifiedNaturalPersonOid = "0.4.0.194112.1.0";

    /// <summary>
    /// ETSI QCP-l: a legal person's qualified certificate.
    /// </summary>
    private const string QualifiedLegalPersonOid = "0.4.0.194112.1.1";

    /// <summary>
    /// ETSI QCP-n-qscd: a natural person's qualified certificate, the key on a qualified device.
    /// </summary>
    private const string QualifiedNaturalPersonDeviceOid = "0.4.0.194112.1.2";

    /// <summary>
    /// ETSI QCP-l-qscd: a legal person's qualified certificate, the key on a qualified device.
    /// </summary>
    private const string QualifiedLegalPersonDeviceOid = "0.4.0.194112.1.3";

    private const string TimestampTokenOid = "1.2.840.113549.1.9.16.2.14";

    private const string SigningTimeOid = "1.2.840.113549.1.9.5";

    private static readonly string[] KnownAuthorities = ["PostSignum", "I.CA", "eIdentity"];

    public static FileSignersResponse Read(ReadOnlySpan<byte> cms)
    {
        var signed = new SignedCms();

        try
        {
            signed.Decode(cms);
        }
        catch (CryptographicException)
        {
            return new FileSignersResponse { Problem = FileSignersProblem.NotSigned, Signers = [] };
        }

        var signerInfos = signed.SignerInfos;
        if (signerInfos.Count > FileLimits.MaxSigners)
            return new FileSignersResponse { Problem = FileSignersProblem.TooManySigners, Signers = [] };

        try
        {
            // Parsed once: SignerInfo.Certificate parses every certificate the file carries on each call.
            var certificates = signed.Certificates;

            return new FileSignersResponse
            {
                Signers =
                [
                    .. signerInfos
                        .Cast<SignerInfo>()
                        .Select(signer => Signer(signer, certificates.FirstOrDefault(signer.SignerIdentifier.MatchesCertificate))),
                ],
            };
        }
        catch (CryptographicException)
        {
            return new FileSignersResponse { Problem = FileSignersProblem.NotSigned, Signers = [] };
        }
    }

    private static FileSigner Signer(SignerInfo signer, X509Certificate2? certificate)
    {

        return new FileSigner
        {
            Name = certificate?.GetNameInfo(X509NameType.SimpleName, forIssuer: false) ?? "",
            Kind = certificate is null ? FileSignerKind.Other : Kind(certificate),
            CertificateIssuer = certificate is null ? null : Authority(certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: true)),
            CertificateValidFrom = certificate?.NotBefore.ToUniversalTime(),
            CertificateValidTo = certificate?.NotAfter.ToUniversalTime(),
            Timestamped = Timestamp(signer),
            SigningTime = SigningTime(signer),
        };
    }

    private static FileSignerKind Kind(X509Certificate2 certificate)
    {
        var policies = Policies(certificate);

        if (policies.Contains(QualifiedNaturalPersonDeviceOid, StringComparer.Ordinal))
            return FileSignerKind.QualifiedSignature;

        if (policies.Contains(QualifiedLegalPersonDeviceOid, StringComparer.Ordinal))
            return FileSignerKind.QualifiedSeal;

        if (policies.Contains(QualifiedNaturalPersonOid, StringComparer.Ordinal))
            return FileSignerKind.QualifiedCertificateSignature;

        if (policies.Contains(QualifiedLegalPersonOid, StringComparer.Ordinal))
            return FileSignerKind.QualifiedCertificateSeal;

        return FileSignerKind.Other;
    }

    private static List<string> Policies(X509Certificate2 certificate)
    {
        var extension = certificate.Extensions[CertificatePoliciesOid];
        if (extension is null)
            return [];

        // certificatePolicies ::= SEQUENCE OF PolicyInformation, PolicyInformation ::= SEQUENCE { policyIdentifier, qualifiers OPTIONAL }; read for display only.
        var oids = new List<string>();

        try
        {
            var policies = new AsnReader(extension.RawData, AsnEncodingRules.DER).ReadSequence();
            while (policies.HasData)
                oids.Add(policies.ReadSequence().ReadObjectIdentifier());
        }
        catch (AsnContentException)
        {
            return [];
        }

        return oids;
    }

    private static string Authority(string commonName)
    {
        return KnownAuthorities.FirstOrDefault(authority => commonName.StartsWith(authority, StringComparison.Ordinal)) ?? commonName;
    }

    private static DateTime? Timestamp(SignerInfo signer)
    {
        var attribute = signer.UnsignedAttributes
            .Cast<CryptographicAttributeObject>()
            .FirstOrDefault(static attribute => string.Equals(attribute.Oid.Value, TimestampTokenOid, StringComparison.Ordinal));

        if (attribute is not { Values.Count: > 0 })
            return null;

        return Rfc3161TimestampToken.TryDecode(attribute.Values[0].RawData, out var token, out _)
            ? token.TokenInfo.Timestamp.UtcDateTime
            : null;
    }

    private static DateTime? SigningTime(SignerInfo signer)
    {
        var attribute = signer.SignedAttributes
            .Cast<CryptographicAttributeObject>()
            .FirstOrDefault(static attribute => string.Equals(attribute.Oid.Value, SigningTimeOid, StringComparison.Ordinal));

        if (attribute is not { Values.Count: > 0 })
            return null;

        try
        {
            var stated = new Pkcs9SigningTime(attribute.Values[0].RawData).SigningTime;

            return stated.Kind == DateTimeKind.Local ? stated.ToUniversalTime() : DateTime.SpecifyKind(stated, DateTimeKind.Utc);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }
}

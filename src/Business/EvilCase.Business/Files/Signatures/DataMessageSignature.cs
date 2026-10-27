using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Xml;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Verifies the ISDS seal of a ZFO: a CMS SignedData over the ISDS XML, sealed by the ISDS with a PostSignum certificate,
/// with a qualified timestamp over the seal as an unsigned attribute. The seal is judged at that timestamp.
/// </summary>
internal static class DataMessageSignature
{
    /// <summary>
    /// The ISDS operator, Digitální a informační agentura.
    /// </summary>
    private const string IsdsOrganizationIdentifier = "NTRCZ-17651921";

    /// <summary>
    /// The former ISDS operator, Ministerstvo vnitra, which sealed the older messages.
    /// </summary>
    private const string FormerIsdsOrganizationIdentifier = "NTRCZ-00007064";

    /// <summary>
    /// The seal's common name; ISDS writes the environment after it, with either dash.
    /// </summary>
    private const string IsdsCommonNamePrefix = "Informační systém datových schránek";

    private const string CommonNameOid = "2.5.4.3";

    private const string OrganizationIdentifierOid = "2.5.4.97";

    /// <summary>
    /// ETSI QCP-l: a qualified certificate of a legal person, which a seal is and a person's certificate is not.
    /// </summary>
    private const string QualifiedLegalPersonPolicyOid = "0.4.0.194112.1.1";

    private const X509ChainStatusFlags PolicyFlags =
        X509ChainStatusFlags.NotValidForUsage | X509ChainStatusFlags.InvalidPolicyConstraints | X509ChainStatusFlags.NoIssuanceChainPolicy;

    private const string TimestampTokenOid = "1.2.840.113549.1.9.16.2.14";

    private const string TimeStampingOid = "1.3.6.1.5.5.7.3.8";

    private static readonly XmlReaderSettings XmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
    };

    public static FileSignatureResponse Verify(ReadOnlySpan<byte> zfo, SignatureTrust trust, ISignatureRevocation revocation)
    {
        var cms = new SignedCms();

        try
        {
            cms.Decode(zfo);
        }
        catch (CryptographicException)
        {
            return Invalid(FileSignatureProblem.NotDataMessage);
        }

        if (cms.Detached || cms.SignerInfos.Count != 1)
            return Invalid(FileSignatureProblem.NotDataMessage);

        var signer = cms.SignerInfos[0];

        // ISDS always carries the seal certificate; without it nothing says who sealed.
        if (signer.Certificate is not { } certificate)
            return Invalid(FileSignatureProblem.NotDataMessage);

        try
        {
            signer.CheckSignature(verifySignatureOnly: true);
        }
        catch (CryptographicException)
        {
            return Invalid(FileSignatureProblem.ContentMismatch);
        }

        if (!IsEnvelope(cms.ContentInfo.Content))
            return Invalid(FileSignatureProblem.NotDataMessage);

        if (TrustedTime(signer, trust) is not { } timestamped)
            return Invalid(FileSignatureProblem.TimestampInvalid);

        using (var chain = trust.NewChain())
        {
            chain.ChainPolicy.VerificationTime = timestamped;
            chain.ChainPolicy.CertificatePolicy.Add(new Oid(QualifiedLegalPersonPolicyOid));

            if (!chain.Build(certificate))
                return Invalid(ChainProblem(chain));
        }

        if (!IsIsds(certificate))
            return Invalid(FileSignatureProblem.NotIsdsSigner);

        var revocationStatus = revocation.CheckRevocation(certificate, trust);

        return revocationStatus.Status switch
        {
            SignatureRevocationStatus.NotRevoked => Verified(FileSignatureState.Valid, certificate, timestamped, revocationStatus.Checked),
            SignatureRevocationStatus.Revoked => Invalid(FileSignatureProblem.Revoked),
            SignatureRevocationStatus.Unknown => Verified(FileSignatureState.Unverifiable, certificate, timestamped, revocationChecked: null),
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// A chain that leaves the bundled hierarchy is not PostSignum's; inside it, a certificate out of its validity, then one
    /// without the seal policy.
    /// </summary>
    private static FileSignatureProblem ChainProblem(X509Chain chain)
    {
        var flags = chain.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, static (all, status) => all | status.Status);

        if ((flags & ~(X509ChainStatusFlags.NotTimeValid | PolicyFlags)) != X509ChainStatusFlags.NoError)
            return FileSignatureProblem.NotPostSignum;

        return flags.HasFlag(X509ChainStatusFlags.NotTimeValid) ? FileSignatureProblem.NotValidAtTimestamp : FileSignatureProblem.NotIsdsSigner;
    }

    /// <summary>
    /// The time of the timestamp token over the signature, or null where the token is missing, does not cover this signature,
    /// or its TSA does not chain to the trust's roots at that time.
    /// </summary>
    private static DateTime? TrustedTime(SignerInfo signer, SignatureTrust trust)
    {
        var attribute = signer.UnsignedAttributes
            .Cast<CryptographicAttributeObject>()
            .FirstOrDefault(static attribute => string.Equals(attribute.Oid.Value, TimestampTokenOid, StringComparison.Ordinal));

        if (attribute is not { Values.Count: > 0 })
            return null;

        if (!Rfc3161TimestampToken.TryDecode(attribute.Values[0].RawData, out var token, out _))
            return null;

        if (!token.VerifySignatureForSignerInfo(signer, out var tsaCertificate, trust.Intermediates) || tsaCertificate is null)
            return null;

        var timestamped = token.TokenInfo.Timestamp.UtcDateTime;

        using var chain = trust.NewChain();
        chain.ChainPolicy.VerificationTime = timestamped;
        chain.ChainPolicy.ApplicationPolicy.Add(new Oid(TimeStampingOid));

        return chain.Build(tsaCertificate) ? timestamped : null;
    }

    /// <summary>
    /// Whether the signed content is ISDS XML holding a data message or a delivery receipt.
    /// </summary>
    private static bool IsEnvelope(byte[] content)
    {
        try
        {
            using var reader = XmlReader.Create(new MemoryStream(content, writable: false), XmlSettings);

            while (reader.Read())
            {
                if (reader.NodeType == XmlNodeType.Element && reader.LocalName is "dmReturnedMessage" or "dmDelivery")
                    return true;
            }

            return false;
        }
        catch (XmlException)
        {
            return false;
        }
    }

    /// <summary>
    /// Whether the subject's only organizationIdentifier names the ISDS operator, present or former, and its only common name
    /// the ISDS.
    /// </summary>
    private static bool IsIsds(X509Certificate2 certificate)
    {
        return SubjectValues(certificate, OrganizationIdentifierOid) is [IsdsOrganizationIdentifier or FormerIsdsOrganizationIdentifier]
            && SubjectValues(certificate, CommonNameOid) is [{ } commonName]
            && commonName.StartsWith(IsdsCommonNamePrefix, StringComparison.Ordinal);
    }

    private static List<string?> SubjectValues(X509Certificate2 certificate, string oid)
    {
        return
        [
            .. certificate.SubjectName
                .EnumerateRelativeDistinguishedNames()
                .Where(static name => !name.HasMultipleElements)
                .Where(name => string.Equals(name.GetSingleElementType().Value, oid, StringComparison.Ordinal))
                .Select(static name => name.GetSingleElementValue()),
        ];
    }

    private static FileSignatureResponse Verified(FileSignatureState state, X509Certificate2 certificate, DateTime timestamped, DateTime? revocationChecked)
    {
        return new FileSignatureResponse
        {
            State = state,
            CertificateIssuer = certificate.GetNameInfo(X509NameType.SimpleName, forIssuer: true),
            CertificateValidFrom = certificate.NotBefore.ToUniversalTime(),
            CertificateValidTo = certificate.NotAfter.ToUniversalTime(),
            Timestamped = timestamped,
            RevocationChecked = revocationChecked,
        };
    }

    private static FileSignatureResponse Invalid(FileSignatureProblem problem)
    {
        return new FileSignatureResponse { State = FileSignatureState.Invalid, Problem = problem };
    }
}

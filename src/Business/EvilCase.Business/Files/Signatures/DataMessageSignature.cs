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
    /// The start of the seal's common name; ISDS writes the environment after it, with either dash.
    /// </summary>
    private const string IsdsCommonNamePrefix = "Informační systém datových schránek";

    /// <summary>
    /// The end of the seal's common name in the production environment.
    /// </summary>
    private const string IsdsCommonNameSuffix = "produkční prostředí";

    private const string CommonNameOid = "2.5.4.3";

    private const string OrganizationIdentifierOid = "2.5.4.97";

    /// <summary>
    /// ETSI QCP-l: a qualified certificate of a legal person, which a seal is and a person's certificate is not.
    /// </summary>
    private const string QualifiedLegalPersonPolicyOid = "0.4.0.194112.1.1";

    private static readonly XmlReaderSettings XmlSettings = new()
    {
        DtdProcessing = DtdProcessing.Prohibit,
        XmlResolver = null,
    };

    public static FileSignatureResponse Verify(ReadOnlyMemory<byte> zfo, SignatureTrust trust, ISignatureRevocation revocation)
    {
        var cms = new SignedCms();

        try
        {
            cms.Decode(zfo.Span);
        }
        catch (CryptographicException)
        {
            return Invalid(FileSignatureProblem.NotDataMessage);
        }

        if (cms.SignerInfos.Count != 1 || DetachedSignatures.IsDetached(zfo))
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

        if (SignatureTimestamp.TrustedTime(signer, trust) is not { } timestamped)
            return Invalid(FileSignatureProblem.TimestampInvalid);

        if (ChainProblem(certificate, timestamped, trust) is { } chainProblem)
            return Invalid(chainProblem);

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
    /// Why the seal does not chain at <paramref name="timestamped"/> through bundled certificates to a bundled root with the
    /// seal policy, or null where it does.
    /// </summary>
    /// <remarks>
    /// A seal that does not chain to the bundled hierarchy is not PostSignum's; inside it, one out of its validity, then one
    /// without the seal policy.
    /// </remarks>
    private static FileSignatureProblem? ChainProblem(X509Certificate2 certificate, DateTime timestamped, SignatureTrust trust)
    {
        if (trust.Chains(certificate, timestamped, certificatePolicy: QualifiedLegalPersonPolicyOid))
            return null;

        if (!trust.ChainsAtIssuance(certificate))
            return FileSignatureProblem.NotPostSignum;

        return trust.Chains(certificate, timestamped) ? FileSignatureProblem.NotIsdsSigner : FileSignatureProblem.NotValidAtTimestamp;
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
    /// the ISDS in production.
    /// </summary>
    private static bool IsIsds(X509Certificate2 certificate)
    {
        return SubjectValues(certificate, OrganizationIdentifierOid) is [IsdsOrganizationIdentifier or FormerIsdsOrganizationIdentifier]
            && SubjectValues(certificate, CommonNameOid) is [{ } commonName]
            && commonName.StartsWith(IsdsCommonNamePrefix, StringComparison.Ordinal)
            && commonName.EndsWith(IsdsCommonNameSuffix, StringComparison.Ordinal);
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

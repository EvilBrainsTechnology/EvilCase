using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Api.Contract.Files;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Verifies each signer of a signed file against the qualified trust: the content, a qualified certificate of a bundled
/// authority, the timestamp where there is one, the time and the revocation.
/// </summary>
internal static class SignedFileSignature
{
    /// <summary>
    /// ETSI QCP-n, QCP-l, QCP-n-qscd and QCP-l-qscd.
    /// </summary>
    private static readonly string[] QualifiedPolicies = ["0.4.0.194112.1.0", "0.4.0.194112.1.1", "0.4.0.194112.1.2", "0.4.0.194112.1.3"];

    /// <summary>
    /// The signers of <paramref name="cms"/> with their verdicts; a detached signature is verified against
    /// <paramref name="signedContent"/>.
    /// </summary>
    public static FileSignersResponse Verify(ReadOnlySpan<byte> cms, byte[]? signedContent, SignatureTrust trust, ISignatureRevocation revocation, DateTime now)
    {
        var signed = signedContent is null ? new SignedCms() : new SignedCms(new ContentInfo(signedContent), detached: true);

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
                        .Select(info => Verified(info, certificates.FirstOrDefault(info.SignerIdentifier.MatchesCertificate), trust, revocation, now)),
                ],
            };
        }
        catch (CryptographicException)
        {
            return new FileSignersResponse { Problem = FileSignersProblem.NotSigned, Signers = [] };
        }
    }

    /// <summary>
    /// The listed signers, each unverifiable for <paramref name="problem"/>; a response with a problem comes back unchanged.
    /// </summary>
    public static FileSignersResponse Unverifiable(FileSignersResponse response, FileSignerProblem problem)
    {
        if (response.Problem is not null)
            return response;

        return response with
        {
            Signers = [.. response.Signers.Select(signer => signer with { State = FileSignatureState.Unverifiable, Problem = problem })],
        };
    }

    private static FileSigner Verified(SignerInfo info, X509Certificate2? certificate, SignatureTrust trust, ISignatureRevocation revocation, DateTime now)
    {
        var signer = CmsSigners.Signer(info, certificate);

        if (certificate is null)
            return signer with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.CertificateMissing };

        try
        {
            info.CheckSignature(verifySignatureOnly: true);
        }
        catch (CryptographicException)
        {
            return signer with { State = FileSignatureState.Invalid, Problem = FileSignerProblem.ContentMismatch };
        }

        if (!IsQualified(certificate, trust))
            return signer with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.UntrustedIssuer };

        DateTime? timestamped = null;
        if (SignatureTimestamp.IsPresent(info))
        {
            timestamped = SignatureTimestamp.TrustedTime(info, trust);
            if (timestamped is null)
                return signer with { State = FileSignatureState.Invalid, Problem = FileSignerProblem.TimestampInvalid };
        }

        using (var chain = trust.NewChain())
        {
            chain.ChainPolicy.VerificationTime = timestamped ?? now;

            if (!chain.Build(certificate))
            {
                return timestamped is null
                    ? signer with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.NotValidNow }
                    : signer with { State = FileSignatureState.Invalid, Problem = FileSignerProblem.NotValidAtTimestamp };
            }
        }

        var status = revocation.CheckRevocation(certificate, trust);

        return status.Status switch
        {
            SignatureRevocationStatus.NotRevoked => signer with { State = FileSignatureState.Valid, RevocationChecked = status.Checked },
            SignatureRevocationStatus.Revoked => signer with { State = FileSignatureState.Invalid, Problem = FileSignerProblem.Revoked },
            SignatureRevocationStatus.Unknown => signer with { State = FileSignatureState.Unverifiable, Problem = FileSignerProblem.RevocationUnknown },
            _ => throw new UnreachableException(),
        };
    }

    /// <summary>
    /// Whether the certificate declares a qualified policy and chains to a bundled root through the bundled intermediates, at
    /// any time.
    /// </summary>
    private static bool IsQualified(X509Certificate2 certificate, SignatureTrust trust)
    {
        if (!CmsSigners.Policies(certificate).Exists(static policy => QualifiedPolicies.Contains(policy, StringComparer.Ordinal)))
            return false;

        // The policy is read from the certificate, not required of the chain: eIdentity's CAs declare only their own policy.
        using var chain = trust.NewChain();
        chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;

        return chain.Build(certificate);
    }
}

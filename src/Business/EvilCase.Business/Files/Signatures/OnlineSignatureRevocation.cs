using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Logging;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Asks the issuers named in the chain's certificates (OCSP or CRL) for today's status.
/// </summary>
internal sealed class OnlineSignatureRevocation(TimeProvider timeProvider, ILogger<OnlineSignatureRevocation> logger) : ISignatureRevocation
{
    private static readonly TimeSpan UrlRetrievalTimeout = TimeSpan.FromSeconds(10);

    public SignatureRevocation CheckRevocation(X509Certificate2 certificate, SignatureTrust trust)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;

        using var chain = trust.NewChain();
        var policy = chain.ChainPolicy;

        // A list fetched today speaks of today, so the chain is built now; the certificate may have expired since its timestamp.
        policy.VerificationTime = now;
        policy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
        policy.RevocationMode = X509RevocationMode.Online;
        policy.RevocationFlag = X509RevocationFlag.ExcludeRoot;
        policy.UrlRetrievalTimeout = UrlRetrievalTimeout;

        // ChainStatus still lists NotTimeValid under IgnoreNotTimeValid; only Build's answer applies the flag.
        if (chain.Build(certificate))
            return new SignatureRevocation { Status = SignatureRevocationStatus.NotRevoked, Checked = now };

        var flags = chain.ChainStatus.Aggregate(X509ChainStatusFlags.NoError, static (all, status) => all | status.Status);

        if (flags.HasFlag(X509ChainStatusFlags.Revoked))
            return new SignatureRevocation { Status = SignatureRevocationStatus.Revoked, Checked = now };

        logger.LogWarning("Revocation status of certificate {Thumbprint} is unknown, chain status {ChainStatus}", certificate.Thumbprint, flags);

        return new SignatureRevocation { Status = SignatureRevocationStatus.Unknown, Checked = now };
    }
}

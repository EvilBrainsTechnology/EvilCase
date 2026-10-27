using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.X509;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// Handles the extended key usage of every certificate in a path: where <paramref name="purpose"/> is given, a certificate
/// that restricts its usage must list it or any usage.
/// </summary>
internal sealed class ExtendedKeyUsageChecker(string? purpose) : PkixCertPathChecker
{
    public override void Init(bool forward) { }

    public override bool IsForwardCheckingSupported()
    {
        return true;
    }

    public override ISet<string> GetSupportedExtensions()
    {
        return new HashSet<string>(StringComparer.Ordinal) { X509Extensions.ExtendedKeyUsage.Id };
    }

    public override void Check(X509Certificate cert, ISet<string> unresolvedCritExts)
    {
        unresolvedCritExts.Remove(X509Extensions.ExtendedKeyUsage.Id);

        if (purpose is null || cert.GetExtendedKeyUsage() is not { } usages)
            return;

        if (!usages.Any(usage => usage.Equals(KeyPurposeID.AnyExtendedKeyUsage) || string.Equals(usage.Id, purpose, StringComparison.Ordinal)))
            throw new PkixCertPathValidatorException($"The certificate's extended key usage does not list {purpose}.");
    }
}

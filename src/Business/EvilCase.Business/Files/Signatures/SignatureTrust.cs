using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Org.BouncyCastle.Pkix;
using Org.BouncyCastle.Utilities.Collections;
using Org.BouncyCastle.X509.Store;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

/// <summary>
/// The certificates a signature is trusted against: the roots anchor every chain, the intermediates are the only
/// candidates between a root and the seal. Certificates a signed file carries are never candidates.
/// </summary>
internal sealed class SignatureTrust(X509Certificate2Collection roots, X509Certificate2Collection intermediates)
{
    /// <summary>
    /// The DI key of the trust a signed file is verified against.
    /// </summary>
    public const string QualifiedKey = "Qualified";

    private const string ResourcePrefix = "EvilBrains.EvilCase.Business.Files.Signatures.Trust.";

    public X509Certificate2Collection Roots { get; } = roots;

    public X509Certificate2Collection Intermediates { get; } = intermediates;

    private HashSet<string> Bundled { get; } = [.. roots.Concat(intermediates).Select(static certificate => certificate.GetCertHashString(HashAlgorithmName.SHA256))];

    private HashSet<TrustAnchor> Anchors { get; } = [.. roots.Select(static root => new TrustAnchor(new Org.BouncyCastle.X509.X509Certificate(root.RawData), nameConstraints: null))];

    private List<Org.BouncyCastle.X509.X509Certificate> BundledIntermediates { get; } = [.. intermediates.Select(static intermediate => new Org.BouncyCastle.X509.X509Certificate(intermediate.RawData))];

    /// <summary>
    /// The PostSignum qualified roots and intermediates bundled with the application.
    /// </summary>
    public static SignatureTrust LoadPostSignum()
    {
        return Load(["PostSignum", "PostSignumRetired"]);
    }

    /// <summary>
    /// The roots and intermediates of the Czech qualified trust service providers PostSignum, the retired PostSignum
    /// hierarchy included, I.CA and eIdentity bundled with the application.
    /// </summary>
    public static SignatureTrust LoadQualified()
    {
        return Load(["PostSignum", "PostSignumRetired", "ICA", "eIdentity"]);
    }

    /// <summary>
    /// Whether <paramref name="certificate"/> chains to a bundled root through bundled intermediates only, every certificate
    /// valid at <paramref name="at"/>; where given, the path carries <paramref name="certificatePolicy"/> and every
    /// certificate that restricts its usage lists <paramref name="applicationPolicy"/>.
    /// </summary>
    public bool Chains(X509Certificate2 certificate, DateTime at, string? certificatePolicy = null, string? applicationPolicy = null)
    {
        return this.Chains(new Org.BouncyCastle.X509.X509Certificate(certificate.RawData), at, certificatePolicy, applicationPolicy);
    }

    /// <summary>
    /// Whether <paramref name="certificate"/> chains to a bundled root through bundled intermediates only, every certificate
    /// valid when <paramref name="certificate"/> became valid.
    /// </summary>
    public bool ChainsAtIssuance(X509Certificate2 certificate)
    {
        var target = new Org.BouncyCastle.X509.X509Certificate(certificate.RawData);

        return this.Chains(target, target.NotBefore, certificatePolicy: null, applicationPolicy: null);
    }

    /// <summary>
    /// A chain against the bundled certificates with no download, for an online revocation check. The platform adds the
    /// host's intermediates as candidates; the caller checks the result with <see cref="IsWithinBundle"/>.
    /// </summary>
    public X509Chain NewChain()
    {
        var chain = new X509Chain();
        var policy = chain.ChainPolicy;

        policy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        policy.CustomTrustStore.AddRange(this.Roots);
        policy.ExtraStore.AddRange(this.Intermediates);
        policy.DisableCertificateDownloads = true;

        return chain;
    }

    /// <summary>
    /// Whether every certificate <paramref name="chain"/> found above its first element is bundled, built or not.
    /// </summary>
    public bool IsWithinBundle(X509Chain chain)
    {
        return chain.ChainElements.Skip(1).All(element => this.IsBundled(element.Certificate.RawData));
    }

    private bool Chains(Org.BouncyCastle.X509.X509Certificate target, DateTime at, string? certificatePolicy, string? applicationPolicy)
    {
        // Not X509Chain: on Linux it takes the user's and the system's intermediates as candidates ahead of the bundled ones.
        var parameters = new PkixBuilderParameters(this.Anchors, new X509CertStoreSelector { Certificate = target })
        {
            Date = at,
            IsRevocationEnabled = false,
        };

        parameters.AddStoreCert(CollectionUtilities.CreateStore([target, .. this.BundledIntermediates]));
        parameters.AddCertPathChecker(new ExtendedKeyUsageChecker(applicationPolicy));

        if (certificatePolicy is not null)
        {
            parameters.SetInitialPolicies(new HashSet<string>(StringComparer.Ordinal) { certificatePolicy });
            parameters.IsExplicitPolicyRequired = true;
        }

        PkixCertPathBuilderResult result;
        try
        {
            result = new PkixCertPathBuilder().Build(parameters);
        }
        catch (PkixCertPathBuilderException)
        {
            return false;
        }

        return this.IsBundled(result.TrustAnchor.TrustedCert.GetEncoded())
            && result.CertPath.Certificates.Skip(1).All(element => this.IsBundled(element.GetEncoded()));
    }

    private bool IsBundled(byte[] certificate)
    {
        return this.Bundled.Contains(Convert.ToHexString(SHA256.HashData(certificate)));
    }

    private static SignatureTrust Load(string[] providers)
    {
        return new SignatureTrust(LoadResources(providers, "Roots"), LoadResources(providers, "Intermediates"));
    }

    private static X509Certificate2Collection LoadResources(string[] providers, string folder)
    {
        var assembly = typeof(SignatureTrust).Assembly;
        var prefixes = providers.Select(provider => $"{ResourcePrefix}{provider}.{folder}.").ToList();
        var certificates = new X509Certificate2Collection();

        foreach (var name in assembly.GetManifestResourceNames().Where(name => prefixes.Exists(prefix => name.StartsWith(prefix, StringComparison.Ordinal))))
        {
            using var stream = assembly.GetManifestResourceStream(name)!;
            using var buffer = new MemoryStream();
            stream.CopyTo(buffer);

            certificates.Add(X509CertificateLoader.LoadCertificate(buffer.ToArray()));
        }

        return certificates;
    }
}

using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

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
    /// An offline chain against the bundled certificates: no download, no revocation check. The caller sets the
    /// verification time.
    /// </summary>
    public X509Chain NewChain()
    {
        var chain = new X509Chain();
        var policy = chain.ChainPolicy;

        policy.TrustMode = X509ChainTrustMode.CustomRootTrust;
        policy.CustomTrustStore.AddRange(this.Roots);
        policy.ExtraStore.AddRange(this.Intermediates);
        policy.DisableCertificateDownloads = true;
        policy.RevocationMode = X509RevocationMode.NoCheck;

        return chain;
    }

    /// <summary>
    /// Whether <paramref name="chain"/> builds for <paramref name="certificate"/> with every certificate above it bundled.
    /// </summary>
    public bool BuildsWithinBundle(X509Chain chain, X509Certificate2 certificate)
    {
        return chain.Build(certificate) && this.IsWithinBundle(chain);
    }

    /// <summary>
    /// Whether every certificate <paramref name="chain"/> found above its first element is bundled, built or not.
    /// </summary>
    public bool IsWithinBundle(X509Chain chain)
    {
        // The platform also offers intermediates of the machine's and the user's stores as candidates.
        return chain.ChainElements.Skip(1).All(element => this.Bundled.Contains(element.Certificate.GetCertHashString(HashAlgorithmName.SHA256)));
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

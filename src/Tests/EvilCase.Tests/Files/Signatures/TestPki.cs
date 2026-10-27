using System.Formats.Asn1;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using EvilBrains.EvilCase.Business.Files.Signatures;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// A root and an intermediate CA that issue seals and timestamp units, all ECDsa P-256.
/// </summary>
internal sealed class TestPki : IDisposable
{
    public const string IsdsOrganizationIdentifier = "NTRCZ-17651921";

    public const string IsdsCommonName = "Informační systém datových schránek – produkční prostředí";

    /// <summary>
    /// ETSI QCP-l, the policy of a qualified seal.
    /// </summary>
    public const string SealPolicy = "0.4.0.194112.1.1";

    private const string AnyPolicy = "2.5.29.32.0";

    private const string TimeStampingOid = "1.3.6.1.5.5.7.3.8";

    private static readonly DateTimeOffset CaFrom = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static readonly DateTimeOffset CaTo = new(2040, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly List<X509Certificate2> issued = [];

    private int serial;

    private TestPki(string name, string caPolicy)
    {
        this.Root = this.Track(CreateRoot($"CN={name} Root", caPolicy));
        this.Intermediate = this.Track(this.IssueCa($"CN={name} Qualified CA", this.Root, caPolicy));
        this.Trust = new SignatureTrust([this.Root], [this.Intermediate]);
    }

    public X509Certificate2 Root { get; }

    public X509Certificate2 Intermediate { get; }

    public SignatureTrust Trust { get; }

    /// <summary>
    /// A PKI whose CAs declare <paramref name="caPolicy"/>, any policy by default.
    /// </summary>
    public static TestPki Create(string name = "Test", string caPolicy = AnyPolicy)
    {
        return new TestPki(name, caPolicy);
    }

    /// <summary>
    /// A seal certificate with the subject and the certificate policy given.
    /// </summary>
    public X509Certificate2 IssueSeal(
        DateTimeOffset notBefore,
        DateTimeOffset notAfter,
        string organizationIdentifier = IsdsOrganizationIdentifier,
        string commonName = IsdsCommonName,
        string policy = SealPolicy)
    {
        var subject = new X500DistinguishedNameBuilder();
        subject.AddCountryOrRegion("CZ");
        subject.Add("2.5.4.97", organizationIdentifier, UniversalTagNumber.UTF8String);
        subject.AddOrganizationName("Testovací agentura");
        subject.AddCommonName(commonName);

        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject.Build(), key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, critical: true));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(this.Intermediate, includeKeyIdentifier: true, includeIssuerAndSerial: false));
        request.CertificateExtensions.Add(CertificatePolicies(policy));

        return this.Track(this.Issue(request, key, this.Intermediate, notBefore, notAfter));
    }

    /// <summary>
    /// A timestamp unit certificate, valid as long as the CAs.
    /// </summary>
    public X509Certificate2 IssueTsa()
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest("CN=Testovací TSA", key, HashAlgorithmName.SHA256);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: false, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, critical: true));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension([new Oid(TimeStampingOid)], critical: true));
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(this.Intermediate, includeKeyIdentifier: true, includeIssuerAndSerial: false));

        return this.Track(this.Issue(request, key, this.Intermediate, CaFrom, CaTo));
    }

    public void Dispose()
    {
        foreach (var certificate in this.issued)
            certificate.Dispose();
    }

    private static X509Certificate2 CreateRoot(string subject, string policy)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
        AddCaExtensions(request, policy);

        return request.CreateSelfSigned(CaFrom, CaTo);
    }

    private static void AddCaExtensions(CertificateRequest request, string policy)
    {
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(certificateAuthority: true, hasPathLengthConstraint: false, pathLengthConstraint: 0, critical: true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.KeyCertSign | X509KeyUsageFlags.CrlSign, critical: true));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, critical: false));
        request.CertificateExtensions.Add(CertificatePolicies(policy));
    }

    // certificatePolicies ::= SEQUENCE OF PolicyInformation, PolicyInformation ::= SEQUENCE { policyIdentifier OBJECT IDENTIFIER }
    private static X509Extension CertificatePolicies(string policy)
    {
        var writer = new AsnWriter(AsnEncodingRules.DER);

        using (writer.PushSequence())
        using (writer.PushSequence())
            writer.WriteObjectIdentifier(policy);

        return new X509Extension("2.5.29.32", writer.Encode(), critical: false);
    }

    private X509Certificate2 IssueCa(string subject, X509Certificate2 issuer, string policy)
    {
        using var key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        var request = new CertificateRequest(subject, key, HashAlgorithmName.SHA256);
        AddCaExtensions(request, policy);
        request.CertificateExtensions.Add(X509AuthorityKeyIdentifierExtension.CreateFromCertificate(issuer, includeKeyIdentifier: true, includeIssuerAndSerial: false));

        return this.Issue(request, key, issuer, CaFrom, CaTo);
    }

    private X509Certificate2 Issue(CertificateRequest request, ECDsa key, X509Certificate2 issuer, DateTimeOffset notBefore, DateTimeOffset notAfter)
    {
        this.serial++;

        using var certificate = request.Create(issuer, notBefore, notAfter, [0x01, (byte)this.serial]);

        return certificate.CopyWithPrivateKey(key);
    }

    private X509Certificate2 Track(X509Certificate2 certificate)
    {
        this.issued.Add(certificate);

        return certificate;
    }
}

using System.Security.Cryptography.X509Certificates;

namespace EvilBrains.EvilCase.Business.Files.Signatures;

internal interface ISignatureRevocation
{
    /// <summary>
    /// The revocation status today of a certificate that already chained to the trust's roots.
    /// </summary>
    public SignatureRevocation CheckRevocation(X509Certificate2 certificate, SignatureTrust trust);
}

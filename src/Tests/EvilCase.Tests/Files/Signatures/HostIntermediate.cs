using System.Security.Cryptography.X509Certificates;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// An intermediate CA in the current user's intermediate store, which the platform offers to every chain, until disposed.
/// </summary>
internal sealed class HostIntermediate : IDisposable
{
    private readonly X509Certificate2 certificate;

    public HostIntermediate(X509Certificate2 certificate)
    {
        using var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Add(certificate);

        this.certificate = certificate;
    }

    public void Dispose()
    {
        using var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);
        store.Remove(this.certificate);
    }
}

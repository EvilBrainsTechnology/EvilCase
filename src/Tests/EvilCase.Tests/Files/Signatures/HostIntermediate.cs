using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// An intermediate CA in the current user's intermediate store, which the platform offers to every chain, until disposed.
/// </summary>
internal sealed class HostIntermediate : IDisposable
{
    private readonly X509Certificate2 certificate;

    private HostIntermediate(X509Certificate2 certificate)
    {
        this.certificate = certificate;
    }

    /// <summary>
    /// Adds the public part of <paramref name="certificate"/>; ignores the test where the platform has no writable store.
    /// </summary>
    public static HostIntermediate Add(X509Certificate2 certificate)
    {
        var publicOnly = X509CertificateLoader.LoadCertificate(certificate.RawData);

        try
        {
            using var store = Open();
            store.Add(publicOnly);
        }
        catch (Exception exception) when (exception is PlatformNotSupportedException or CryptographicException)
        {
            publicOnly.Dispose();
            Assert.Ignore($"The current user's intermediate store is not writable: {exception.Message}");
        }

        return new HostIntermediate(publicOnly);
    }

    public void Dispose()
    {
        using (var store = Open())
            store.Remove(this.certificate);

        this.certificate.Dispose();
    }

    private static X509Store Open()
    {
        var store = new X509Store(StoreName.CertificateAuthority, StoreLocation.CurrentUser);
        store.Open(OpenFlags.ReadWrite);

        return store;
    }
}

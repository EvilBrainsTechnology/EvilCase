using System.Net;
using System.Net.Sockets;

namespace EvilBrains.EvilCase.Tests.Files.Signatures;

/// <summary>
/// Serves CRLs over HTTP on the loopback until disposed.
/// </summary>
internal sealed class CrlServer : IDisposable
{
    private readonly HttpListener listener = new();

    private readonly Dictionary<string, byte[]> crls = new(StringComparer.Ordinal);

    private readonly Task serving;

    public CrlServer()
    {
        this.BaseUri = new UriBuilder(Uri.UriSchemeHttp, IPAddress.Loopback.ToString(), FreePort()).Uri;
        this.listener.Prefixes.Add(this.BaseUri.AbsoluteUri);
        this.listener.Start();
        this.serving = Task.Run(this.ServeAsync);
    }

    public Uri BaseUri { get; }

    public void Publish(string name, byte[] crl)
    {
        lock (this.crls)
            this.crls[name] = crl;
    }

    public void Dispose()
    {
        this.listener.Close();
        this.serving.Wait(TimeSpan.FromSeconds(5));
    }

    private static int FreePort()
    {
        using var socket = new TcpListener(IPAddress.Loopback, 0);
        socket.Start();

        return ((IPEndPoint)socket.LocalEndpoint).Port;
    }

    private async Task ServeAsync()
    {
        while (this.listener.IsListening)
        {
            HttpListenerContext context;
            try
            {
                context = await this.listener.GetContextAsync();
            }
            catch (Exception exception) when (exception is HttpListenerException or ObjectDisposedException or InvalidOperationException)
            {
                return;
            }

            byte[]? crl;
            lock (this.crls)
                this.crls.TryGetValue(context.Request.Url!.AbsolutePath.TrimStart('/'), out crl);

            using var response = context.Response;
            if (crl is null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                continue;
            }

            response.ContentType = "application/pkix-crl";
            await response.OutputStream.WriteAsync(crl);
        }
    }
}

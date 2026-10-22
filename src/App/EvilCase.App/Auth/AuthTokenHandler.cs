using System.Net;
using System.Net.Http.Headers;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class AuthTokenHandler(ISessionStore store, IServiceProvider services) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Both tokens are cookies and fetch only sends or stores one when it is asked to.
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        // Renewing on behalf of these comes back here, so they are sent as they are.
        if (IsExemptFromRenewal(request))
            return await base.SendAsync(request, cancellationToken);

        // A file upload is too large to hold in memory for a retry, so it goes out once.
        var upload = request.Content is MultipartContent;

        // Buffered before the first attempt: HttpClient disposes the content it sent, so a retry would
        // have nothing left to send.
        var body = upload || request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = request.Content?.Headers.ContentType;

        await this.Session().EnsureFresh(cancellationToken);

        // A 401 on this session renews even while it looks fresh: another tab may have signed out.
        var sent = store.Current;

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || store.Current is null)
            return response;

        if (!await this.Session().Renew(sent, cancellationToken) || upload)
            return response;

        response.Dispose();

        using var retry = Clone(request, body, contentType);

        return await base.SendAsync(retry, cancellationToken);
    }

    /// <summary>
    /// Mirrors the server's [AllowAnonymous] list; the log upload renewing through this handler would loop.
    /// </summary>
    private static bool IsExemptFromRenewal(HttpRequestMessage request)
    {
        return IsUnder(request, AuthRoute.LoginPath)
            || IsUnder(request, AuthRoute.RefreshPath)
            || IsUnder(request, AuthRoute.LogoutPath)
            || IsUnder(request, ClientLogRoute.Path);
    }

    // By segment rather than by characters, the way the host partitions the same paths: a plain prefix
    // would swallow a future /api/authors too, and silently stop renewing for all of it.
    private static bool IsUnder(HttpRequestMessage request, string path)
    {
        return request.RequestUri?.AbsolutePath is { } absolute
            && absolute.StartsWith(path, StringComparison.OrdinalIgnoreCase)
            && (absolute.Length == path.Length || absolute[path.Length] == '/');
    }

    private static HttpRequestMessage Clone(HttpRequestMessage request, byte[]? body, MediaTypeHeaderValue? contentType)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        // Where the browser request options live, the fetch credentials among them: without them the
        // retry would leave the renewed cookies behind.
        foreach (var option in (IDictionary<string, object?>)request.Options)
            clone.Options.Set(new HttpRequestOptionsKey<object?>(option.Key), option.Value);

        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            clone.Content.Headers.ContentType = contentType;
        }

        return clone;
    }

    // Resolved on use rather than taken in the constructor: the session renews through the generated
    // auth client, which is built with this handler in its chain, and asking for it up front is a cycle.
    private IAuthSession Session()
    {
        return services.GetRequiredService<IAuthSession>();
    }
}

using System.Net;
using System.Net.Http.Headers;
using EvilBrains.EvilCase.Api.Contract.Logging;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class AuthTokenHandler(IAccessTokenStore tokens, IServiceProvider services) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // The refresh token is a cookie and fetch only sends one when it is asked to.
        if (IsUnder(request, AuthRoute.Path))
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        // Renewing on behalf of these comes back here, so they carry whatever token is at hand and no more.
        if (IsExemptFromRenewal(request))
        {
            this.Authorize(request);

            return await base.SendAsync(request, cancellationToken);
        }

        // Buffered before the first attempt: HttpClient disposes the content it sent, so a retry would
        // have nothing left to send.
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = request.Content?.Headers.ContentType;

        if (this.IsExpiring())
            await this.Session().Renew(cancellationToken);

        this.Authorize(request);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized || tokens.Current is null)
            return response;

        if (!await this.Session().Renew(cancellationToken))
            return response;

        response.Dispose();

        using var retry = Clone(request, body, contentType);

        this.Authorize(retry);

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
        // retry would leave the refresh cookie behind and ignore the one coming back.
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

    private bool IsExpiring()
    {
        return tokens.Current is { } current
            && current.ExpiresAt - DateTime.UtcNow <= EvilCaseAuthenticationStateProvider.RenewAhead;
    }

    private void Authorize(HttpRequestMessage request)
    {
        if (tokens.Current is { } current)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.Token);
    }
}

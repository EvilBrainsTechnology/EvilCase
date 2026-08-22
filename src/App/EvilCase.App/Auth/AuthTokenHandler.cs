using System.Net;
using System.Net.Http.Headers;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// Puts the access token on every API call and renews it when it is about to run out or has already
/// been refused. Attached to all generated clients, because that is the only hook they offer.
/// </summary>
internal sealed class AuthTokenHandler(IAccessTokenStore tokens, IServiceProvider services) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // The refresh token is a cookie and fetch only sends one when it is asked to.
        if (IsUnder(request, AuthRoute.Path))
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        // Renewal goes through these, so renewing on their behalf would call this handler again.
        if (IsAnonymousAuthEndpoint(request))
        {
            this.Authorize(request);

            return await base.SendAsync(request, cancellationToken);
        }

        // Buffered before the first attempt: HttpClient disposes the content it sent, so a retry would
        // have nothing left to send. Every payload here is small enough for that to cost nothing.
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = request.Content?.Headers.ContentType;

        if (this.IsExpiring())
            await this.Session().Renew(cancellationToken);

        this.Authorize(request);

        var response = await base.SendAsync(request, cancellationToken);

        // Nothing to renew from where the caller was never signed in: the request simply was not allowed.
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
    /// The three endpoints under <c>/api/auth</c> that carry <c>[AllowAnonymous]</c>: signing in has no
    /// session to renew yet, renewal is this handler's own way out of an expired token, and signing out
    /// is about to throw the session away. Everything else under that prefix is <c>[Authorize]</c> and
    /// needs the bearer kept alive like any other endpoint — <c>logout-all</c> above all, whose failure
    /// leaves every other device signed in.
    /// </summary>
    private static bool IsAnonymousAuthEndpoint(HttpRequestMessage request)
    {
        return IsUnder(request, AuthRoute.LoginPath)
            || IsUnder(request, AuthRoute.RefreshPath)
            || IsUnder(request, AuthRoute.LogoutPath);
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

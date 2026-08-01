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
        ArgumentNullException.ThrowIfNull(request);

        // Renewal goes through these, so renewing on their behalf would call this handler again.
        if (IsAuthEndpoint(request))
        {
            // The refresh token is a cookie and fetch only sends one when it is asked to.
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

            this.Authorize(request);

            return await base.SendAsync(request, cancellationToken);
        }

        // Buffered before the first attempt: HttpClient disposes the content it sent, so a retry would
        // have nothing left to send. Every payload here is small enough for that to cost nothing.
        var body = request.Content is null ? null : await request.Content.ReadAsByteArrayAsync(cancellationToken);
        var contentType = request.Content?.Headers.ContentType;

        if (this.IsExpiring())
            _ = await this.Session().RenewAsync(cancellationToken);

        this.Authorize(request);

        var response = await base.SendAsync(request, cancellationToken);

        // Nothing to renew from where the caller was never signed in: the request simply was not allowed.
        if (response.StatusCode != HttpStatusCode.Unauthorized || tokens.Current is null)
            return response;

        if (!await this.Session().RenewAsync(cancellationToken))
            return response;

        response.Dispose();

        using var retry = Clone(request, body, contentType);

        this.Authorize(retry);

        return await base.SendAsync(retry, cancellationToken);
    }

    private static bool IsAuthEndpoint(HttpRequestMessage request) =>
        request.RequestUri?.AbsolutePath.StartsWith(AuthRoute.Path, StringComparison.OrdinalIgnoreCase) == true;

    private static HttpRequestMessage Clone(HttpRequestMessage request, byte[]? body, MediaTypeHeaderValue? contentType)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
            _ = clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (body is not null)
        {
            clone.Content = new ByteArrayContent(body);
            clone.Content.Headers.ContentType = contentType;
        }

        return clone;
    }

    // Resolved on use rather than taken in the constructor: the session renews through the generated
    // auth client, which is built with this handler in its chain, and asking for it up front is a cycle.
    private IAuthSession Session() => services.GetRequiredService<IAuthSession>();

    private bool IsExpiring() =>
        tokens.Current is { } current
            && current.ExpiresAt - DateTime.UtcNow <= EvilCaseAuthenticationStateProvider.RenewAhead;

    private void Authorize(HttpRequestMessage request)
    {
        if (tokens.Current is { } current)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.Token);
    }
}

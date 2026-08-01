using System.Net;
using System.Security.Claims;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Components.Authorization;

namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// Holds the signed-in user for the whole application. The framework resolves it by its base type, so
/// the interface next to it is what everything else talks to.
/// </summary>
internal sealed class EvilCaseAuthenticationStateProvider(
    IAccessTokenStore tokens,
    IAuthClient authClient,
    ILogger<EvilCaseAuthenticationStateProvider> logger) : AuthenticationStateProvider, IAuthSession, IDisposable
{
    private const string AuthenticationType = "EvilCase";

    /// <summary>
    /// Renewing this far ahead of the expiry keeps a request that is already on its way from coming
    /// back 401 because the clock rolled over while it was in flight.
    /// </summary>
    public static readonly TimeSpan RenewAhead = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim renewal = new(initialCount: 1, maxCount: 1);

    private Task<AuthenticationState>? state;

    private bool restored;

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => this.state ??= this.RestoreAsync();

    public void Dispose() => this.renewal.Dispose();

    public async Task<SignInOutcome> SignInAsync(string email, string password, CancellationToken cancellationToken)
    {
        try
        {
            var response = await authClient.Login(new() { Email = email, Password = password }, cancellationToken);

            this.Apply(response);
            this.Publish();

            return SignInOutcome.Success;
        }
        catch (ApiException exception)
        {
            if (exception.StatusCode == HttpStatusCode.Locked)
                return SignInOutcome.LockedOut;

            return exception.StatusCode == HttpStatusCode.TooManyRequests
                ? SignInOutcome.TooManyAttempts
                : SignInOutcome.InvalidCredentials;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            // Nothing here passes a token that can be cancelled, so a cancellation is a timeout.
            logger.LogWarning(exception, "Sign-in could not reach the API");

            return SignInOutcome.Unreachable;
        }
    }

    public async Task SignOutAsync(bool everywhere, CancellationToken cancellationToken)
    {
        try
        {
            if (everywhere)
                await authClient.LogoutAll(cancellationToken);
            else
                await authClient.Logout(cancellationToken);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            // The refresh token may outlive this, but the alternative is refusing to sign the user out
            // of the browser in front of them.
            logger.LogWarning(exception, "Sign-out could not be completed on the server");
        }

        tokens.Clear();
        this.Publish();
    }

    public async Task<bool> RenewAsync(CancellationToken cancellationToken)
    {
        await this.renewal.WaitAsync(cancellationToken);

        try
        {
            // Whoever was ahead in the queue may have done the work already.
            if (tokens.Current is { } current && current.ExpiresAt - DateTime.UtcNow > RenewAhead)
                return true;

            this.Apply(await authClient.Refresh(cancellationToken));

            return true;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            var wasSignedIn = tokens.Current is not null;

            tokens.Clear();

            if (wasSignedIn)
            {
                logger.LogInformation(exception, "The session could not be renewed and was ended");
                this.Publish();
            }

            return false;
        }
        finally
        {
            _ = this.renewal.Release();
        }
    }

    private static AuthenticationState Build(AccessTokenState? token)
    {
        if (token is null)
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        // The claim types match what the API puts in the token, so [Authorize(Roles = ...)] and
        // AuthorizeView mean the same thing on both sides.
        var identity = new ClaimsIdentity(
            [new Claim(AuthClaims.Email, token.Email), new Claim(AuthClaims.Role, token.Role.ToString())],
            AuthenticationType,
            AuthClaims.Email,
            AuthClaims.Role);

        return new(new ClaimsPrincipal(identity));
    }

    /// <summary>
    /// The first thing the application asks for after a page load. The access token went with the tab,
    /// but the refresh cookie did not, so the session is picked up again before anything renders.
    /// </summary>
    private async Task<AuthenticationState> RestoreAsync()
    {
        _ = await this.RenewAsync(CancellationToken.None);

        this.restored = true;

        return Build(tokens.Current);
    }

    private void Apply(LoginResponse response) =>
        tokens.Set(new()
        {
            Token = response.AccessToken,
            ExpiresAt = response.ExpiresAt,
            Email = response.Email,
            Role = response.Role,
        });

    private void Publish()
    {
        var next = Task.FromResult(Build(tokens.Current));

        this.state = next;

        // Nothing is subscribed until the restore has returned, and notifying from inside it would ask
        // the router to re-render a state it has not been handed yet.
        if (this.restored)
            this.NotifyAuthenticationStateChanged(next);
    }
}

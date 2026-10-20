using System.Net;
using System.Security.Claims;
using EvilBrains.ApiClient;
using EvilBrains.EvilCase.Api.Client;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.AspNetCore.Components.Authorization;

namespace EvilBrains.EvilCase.App.Auth;

internal sealed class EvilCaseAuthenticationStateProvider(
    ISessionStore store,
    IAuthClient authClient,
    ILogger<EvilCaseAuthenticationStateProvider> logger) : AuthenticationStateProvider, IAuthSession, IDisposable
{
    private const string AuthenticationType = "EvilCase";

    /// <summary>
    /// The margin covers a request already on its way when the token runs out.
    /// </summary>
    public static readonly TimeSpan RenewAhead = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim renewal = new(initialCount: 1, maxCount: 1);

    private Task<AuthenticationState>? state;

    private bool restored;

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        this.state ??= this.Restore();
        return await this.state;
    }

    public void Dispose()
    {
        this.renewal.Dispose();
    }

    public async Task<SignInOutcome> SignIn(string email, string password, CancellationToken token)
    {
        try
        {
            var response = await authClient.Login(new() { Email = email, Password = password }, token);

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

    public async Task SignOut(bool everywhere, CancellationToken token)
    {
        try
        {
            if (everywhere)
                await authClient.LogoutAll(token);
            else
                await authClient.Logout(token);
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            // The refresh token may outlive this, but the alternative is refusing to sign the user out
            // of the browser in front of them.
            logger.LogWarning(exception, "Sign-out could not be completed on the server");
        }

        store.Clear();
        this.Publish();
    }

    public async Task<bool> Renew(SessionState? seen, CancellationToken token)
    {
        await this.renewal.WaitAsync(token);

        try
        {
            // Whoever was ahead in the queue may have done the work already.
            if (store.Current is { } current && current != seen && current.ExpiresAt - DateTime.UtcNow > RenewAhead)
                return true;

            this.Apply(await authClient.Refresh(token));

            return true;
        }
        catch (ApiException exception) when (exception.StatusCode == HttpStatusCode.Unauthorized)
        {
            var wasSignedIn = store.Current is not null;

            store.Clear();

            if (wasSignedIn)
            {
                logger.LogInformation(exception, "The session was rejected and was ended");
                this.Publish();
            }

            return false;
        }
        catch (Exception exception) when (exception is ApiException or HttpRequestException or TaskCanceledException)
        {
            logger.LogWarning(exception, "The session could not be renewed");

            return false;
        }
        finally
        {
            this.renewal.Release();
        }
    }

    private static AuthenticationState BuildAuthenticationState(SessionState? token)
    {
        if (token is null)
            return new(new ClaimsPrincipal(new ClaimsIdentity()));

        var identity = new ClaimsIdentity(
            [new Claim(AuthClaims.Email, token.Email), new Claim(AuthClaims.Role, token.Role.ToString())],
            AuthenticationType,
            AuthClaims.Email,
            AuthClaims.Role);

        return new(new ClaimsPrincipal(identity));
    }

    private async Task<AuthenticationState> Restore()
    {
        await this.Renew(seen: null, CancellationToken.None);

        this.restored = true;

        return BuildAuthenticationState(store.Current);
    }

    private void Apply(LoginResponse response)
    {
        store.Set(new()
        {
            ExpiresAt = response.ExpiresAt,
            Email = response.Email,
            Role = response.Role,
        });
    }

    private void Publish()
    {
        var next = Task.FromResult(BuildAuthenticationState(store.Current));

        this.state = next;

        // Nothing is subscribed until the restore has returned, and notifying from inside it would ask
        // the router to re-render a state it has not been handed yet.
        if (this.restored)
            this.NotifyAuthenticationStateChanged(next);
    }
}

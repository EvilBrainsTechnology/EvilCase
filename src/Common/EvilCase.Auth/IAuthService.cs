namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Everything the authentication endpoints do. The controller above it only translates results into
/// status codes and moves the refresh token in and out of its cookie.
/// </summary>
public interface IAuthService
{
    public Task<LoginResult> Login(string email, string password, ClientInfo client, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token into a fresh pair. A failure says nothing about why beyond the one thing
    /// the caller has to act on: <see cref="RefreshStatus.Raced"/> means the browser's cookie already
    /// holds the replacement and must be left where it is.
    /// </summary>
    public Task<RefreshResult> Refresh(string refreshToken, ClientInfo client, CancellationToken cancellationToken);

    /// <summary>
    /// Ends the session the token belongs to. Silent about a token it does not recognise.
    /// </summary>
    public Task SignOut(string refreshToken, CancellationToken cancellationToken);

    public Task SignOutEverywhere(long userId, CancellationToken cancellationToken);

    public Task<IReadOnlyList<UserSession>> GetSessions(long userId, CancellationToken cancellationToken);
}

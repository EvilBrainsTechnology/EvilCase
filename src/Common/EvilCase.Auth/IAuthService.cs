namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Everything the authentication endpoints do. The controller above it only translates results into
/// status codes and moves the refresh token in and out of its cookie.
/// </summary>
public interface IAuthService
{
    public Task<LoginResult> LoginAsync(string email, string password, ClientInfo client, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates a refresh token into a fresh pair. Null where the token is unknown, expired, revoked or
    /// belongs to a locked-out account — the caller cannot tell which, and does not need to.
    /// </summary>
    public Task<AuthSession?> RefreshAsync(string refreshToken, ClientInfo client, CancellationToken cancellationToken);

    /// <summary>
    /// Ends the session the token belongs to. Silent about a token it does not recognise.
    /// </summary>
    public Task SignOutAsync(string refreshToken, CancellationToken cancellationToken);

    public Task SignOutEverywhereAsync(long userId, CancellationToken cancellationToken);

    public Task<IReadOnlyList<UserSession>> GetSessionsAsync(long userId, CancellationToken cancellationToken);
}

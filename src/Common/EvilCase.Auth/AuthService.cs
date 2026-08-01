using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

internal sealed class AuthService(
    IUserStore userStore,
    IRefreshTokenStore refreshTokenStore,
    IAuthTokenService authTokenService,
    IOptions<AuthSettings> options,
    TimeProvider timeProvider,
    ILogger<AuthService> logger) : IAuthService
{
    /// <summary>
    /// Two tabs can present the same refresh token at once and only one of them can win. Inside this
    /// window a token rotation has just revoked is that race rather than a replay: the browser's cookie
    /// already holds the replacement, so the loser's next attempt succeeds and nothing is torn down.
    /// </summary>
    private static readonly TimeSpan ReplayGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<LoginResult> LoginAsync(string email, string password, ClientInfo client, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await userStore.FindByEmailAsync(EmailNormalizer.Normalize(email), cancellationToken);

        if (user is null)
        {
            // The same work a real verification costs, so response time does not sort known e-mails
            // from unknown ones.
            PasswordHasher.FakeVerify();
            return LoginResult.Failed(LoginStatus.InvalidCredentials);
        }

        if (user.LockoutEnd > now)
            return LoginResult.Failed(LoginStatus.LockedOut);

        if (!PasswordHasher.Verify(password, user.PasswordHash))
            return await this.RecordFailureAsync(user, now, cancellationToken);

        await userStore.RecordSuccessfulLoginAsync(user.Id, now, cancellationToken);

        var sessionExpires = now.Add(options.Value.RefreshToken.SessionExpiration);
        var session = await this.IssueAsync(user, Guid.NewGuid(), sessionExpires, client, now, cancellationToken);

        logger.LogInformation("User {UserId} signed in", user.Id);

        return LoginResult.Succeeded(session);
    }

    public async Task<RefreshResult> RefreshAsync(string refreshToken, ClientInfo client, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var stored = await refreshTokenStore.FindAsync(RefreshTokenValue.Hash(refreshToken), cancellationToken);

        if (stored is null)
            return RefreshResult.Failed(RefreshStatus.Rejected);

        if (stored.RevokedAt is { } revokedAt)
            return await this.HandleReplayAsync(stored, revokedAt, now, cancellationToken);

        if (stored.Expires <= now || stored.SessionExpires <= now)
            return RefreshResult.Failed(RefreshStatus.Rejected);

        var user = await userStore.FindByIdAsync(stored.UserId, cancellationToken);
        if (user is null || user.LockoutEnd > now)
            return RefreshResult.Failed(RefreshStatus.Rejected);

        // Revoking is what settles a race rather than the read above it: two callers presenting the same
        // token both find it live, and only the one whose update still finds it unrevoked may spend it.
        // Without this the loser would be handed a second live token in the same chain, and a stolen
        // cookie racing the browser would never be seen as the replay it is.
        if (!await refreshTokenStore.RevokeAsync(stored.Id, now, cancellationToken))
            return RefreshResult.Failed(RefreshStatus.Raced);

        var session = await this.IssueAsync(user, stored.SessionId, stored.SessionExpires, client, now, cancellationToken);

        return RefreshResult.Succeeded(session);
    }

    public async Task SignOutAsync(string refreshToken, CancellationToken cancellationToken)
    {
        var stored = await refreshTokenStore.FindAsync(RefreshTokenValue.Hash(refreshToken), cancellationToken);

        if (stored is not null)
            await refreshTokenStore.RevokeSessionAsync(stored.SessionId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
    }

    public async Task SignOutEverywhereAsync(long userId, CancellationToken cancellationToken)
    {
        await refreshTokenStore.RevokeAllAsync(userId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);

        logger.LogInformation("Every session of user {UserId} was revoked", userId);
    }

    public async Task<IReadOnlyList<UserSession>> GetSessionsAsync(long userId, CancellationToken cancellationToken)
    {
        var tokens = await refreshTokenStore.GetActiveAsync(userId, timeProvider.GetUtcNow().UtcDateTime, cancellationToken);
        var starts = await refreshTokenStore.GetSessionStartsAsync(userId, cancellationToken);

        return
        [
            .. tokens.Select(
                token => new UserSession
                {
                    SessionId = token.SessionId,
                    Created = starts[token.SessionId],

                    // The live token was issued the last time this session renewed, and every row behind
                    // it carries its own use as the LastUsed rotation stamped on it when it was spent.
                    LastUsed = token.Created,
                    Expires = token.SessionExpires,
                    IpAddress = token.CreatedByIp,
                    UserAgent = token.UserAgent,
                }),
        ];
    }

    private async Task<LoginResult> RecordFailureAsync(User user, DateTime now, CancellationToken cancellationToken)
    {
        var lockout = options.Value.Lockout;
        var attempts = user.FailedLoginAttempts + 1;
        var lockedOut = attempts >= lockout.MaxFailedAttempts;

        // The counter starts over together with the lockout: past that point the lockout is what limits
        // guessing, and a counter left at its ceiling would lock the account again on the first miss.
        await userStore.RecordFailedLoginAsync(
            user.Id,
            lockedOut ? 0 : attempts,
            lockedOut ? now.Add(lockout.Duration) : null,
            now,
            cancellationToken);

        if (lockedOut)
            logger.LogWarning("User {UserId} was locked out after {Attempts} failed sign-in attempts", user.Id, attempts);

        return LoginResult.Failed(lockedOut ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials);
    }

    private async Task<RefreshResult> HandleReplayAsync(RefreshToken stored, DateTime revokedAt, DateTime now, CancellationToken cancellationToken)
    {
        if (now - revokedAt <= ReplayGracePeriod)
            return RefreshResult.Failed(RefreshStatus.Raced);

        logger.LogWarning(
            "A refresh token of session {SessionId} was replayed {Age} after it was revoked; the session is being ended",
            stored.SessionId,
            now - revokedAt);

        await refreshTokenStore.RevokeSessionAsync(stored.SessionId, now, cancellationToken);

        return RefreshResult.Failed(RefreshStatus.Rejected);
    }

    private async Task<AuthSession> IssueAsync(
        User user,
        Guid sessionId,
        DateTime sessionExpires,
        ClientInfo client,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var value = RefreshTokenValue.Create();
        var expires = now.Add(options.Value.RefreshToken.Expiration);

        // Never past the chain's ceiling: rotating must not be a way to extend a session for ever.
        if (expires > sessionExpires)
            expires = sessionExpires;

        await refreshTokenStore.AddAsync(
            new RefreshToken
            {
                UserId = user.Id,
                SessionId = sessionId,
                TokenHash = RefreshTokenValue.Hash(value),
                Created = now,
                Expires = expires,
                SessionExpires = sessionExpires,
                CreatedByIp = client.IpAddress,
                UserAgent = client.UserAgent,
            },
            cancellationToken);

        var accessToken = authTokenService.Generate(user, sessionId);

        return new()
        {
            AccessToken = accessToken.Value,
            AccessTokenExpires = accessToken.ExpiresAt,
            RefreshToken = value,
            RefreshTokenExpires = expires,
            Email = user.Email,
            Role = user.Role,
        };
    }
}

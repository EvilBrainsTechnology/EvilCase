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
    /// Within this window a revoked token is a two-tab race, not a replay: the cookie already holds the
    /// replacement.
    /// </summary>
    private static readonly TimeSpan ReplayGracePeriod = TimeSpan.FromSeconds(30);

    public async Task<LoginResult> Login(string email, string password, ClientInfo client, CancellationToken token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var user = await userStore.FindByEmail(email, token);

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
            return await this.RecordFailure(user, now, token);

        await userStore.RecordSuccessfulLogin(user.Id, token);

        var sessionExpires = now.Add(options.Value.RefreshToken.SessionExpiration);
        var session = await this.IssueSession(user, Guid.NewGuid(), sessionExpires, client, now, token);

        logger.LogInformation("User {UserId} signed in", user.Id);

        return LoginResult.Succeeded(session);
    }

    public async Task<RefreshResult> Refresh(string refreshToken, ClientInfo client, CancellationToken token)
    {
        var now = timeProvider.GetUtcNow().UtcDateTime;
        var stored = await refreshTokenStore.FindRefreshToken(RefreshTokenValue.Hash(refreshToken), token);

        if (stored is null)
            return RefreshResult.Failed(RefreshStatus.Rejected);

        if (stored.RevokedAt is { } revokedAt)
            return await this.HandleReplay(stored, revokedAt, now, token);

        if (stored.Expires <= now || stored.SessionExpires <= now)
            return RefreshResult.Failed(RefreshStatus.Rejected);

        var user = await userStore.FindById(stored.UserId, token);
        if (user is null || user.LockoutEnd > now)
            return RefreshResult.Failed(RefreshStatus.Rejected);

        // The revoke settles the race, not the read above: only the caller whose update finds
        // the row unrevoked may spend it.
        if (!await refreshTokenStore.RevokeRefreshToken(stored.Id, now, token))
            return RefreshResult.Failed(RefreshStatus.Raced);

        var session = await this.IssueSession(user, stored.AuthSessionId, stored.SessionExpires, client, now, token);

        return RefreshResult.Succeeded(session);
    }

    public async Task SignOut(string refreshToken, CancellationToken token)
    {
        var stored = await refreshTokenStore.FindRefreshToken(RefreshTokenValue.Hash(refreshToken), token);

        if (stored is not null)
            await refreshTokenStore.RevokeSession(stored.AuthSessionId, timeProvider.GetUtcNow().UtcDateTime, token);
    }

    public async Task SignOutEverywhere(Guid userId, CancellationToken token)
    {
        await refreshTokenStore.RevokeAll(userId, timeProvider.GetUtcNow().UtcDateTime, token);

        logger.LogInformation("Every session of user {UserId} was revoked", userId);
    }

    public async Task<IReadOnlyList<UserSession>> GetSessions(Guid userId, CancellationToken token)
    {
        var tokens = await refreshTokenStore.GetActive(userId, timeProvider.GetUtcNow().UtcDateTime, token);
        var starts = await refreshTokenStore.GetSessionStarts(userId, token);

        return
        [
            .. tokens.Select(
                token => new UserSession
                {
                    AuthSessionId = token.AuthSessionId,
                    Created = starts[token.AuthSessionId],

                    // The live token's Created is the last renewal; LastUsed is stamped only on spent rows.
                    LastUsed = token.Created,
                    Expires = token.SessionExpires,
                    IpAddress = token.CreatedByIp,
                    UserAgent = token.UserAgent,
                }),
        ];
    }

    private async Task<LoginResult> RecordFailure(User user, DateTime now, CancellationToken token)
    {
        var lockout = options.Value.Lockout;

        var lockedUntil = await userStore.RecordFailedLogin(
            user.Id,
            lockout.MaxFailedAttempts,
            now.Add(lockout.Duration),
            token);

        var lockedOut = lockedUntil > now;

        if (lockedOut)
            logger.LogWarning("User {UserId} was locked out after {Attempts} failed sign-in attempts", user.Id, lockout.MaxFailedAttempts);

        return LoginResult.Failed(lockedOut ? LoginStatus.LockedOut : LoginStatus.InvalidCredentials);
    }

    private async Task<RefreshResult> HandleReplay(RefreshToken stored, DateTime revokedAt, DateTime now, CancellationToken token)
    {
        if (now - revokedAt <= ReplayGracePeriod)
            return RefreshResult.Failed(RefreshStatus.Raced);

        logger.LogWarning(
            "A refresh token of session {AuthSessionId} was replayed {Age} after it was revoked; the session is being ended",
            stored.AuthSessionId,
            now - revokedAt);

        await refreshTokenStore.RevokeSession(stored.AuthSessionId, now, token);

        return RefreshResult.Failed(RefreshStatus.Rejected);
    }

    private async Task<AuthSession> IssueSession(
        User user,
        Guid authSessionId,
        DateTime sessionExpires,
        ClientInfo client,
        DateTime now,
        CancellationToken token)
    {
        var value = RefreshTokenValue.Create();
        var expires = now.Add(options.Value.RefreshToken.Expiration);

        if (expires > sessionExpires)
            expires = sessionExpires;

        await refreshTokenStore.AddRefreshToken(
            new RefreshToken
            {
                UserId = user.Id,
                AuthSessionId = authSessionId,
                TokenHash = RefreshTokenValue.Hash(value),
                Expires = expires,
                SessionExpires = sessionExpires,
                CreatedByIp = client.IpAddress,
                UserAgent = client.UserAgent,
            },
            token);

        var accessToken = authTokenService.Generate(user, authSessionId);

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

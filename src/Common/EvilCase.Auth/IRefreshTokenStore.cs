using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IRefreshTokenStore
{
    public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken token);

    public Task<RefreshToken?> FindRefreshToken(string tokenHash, CancellationToken token);

    /// <summary>
    /// Spends the token and stores its replacement, both or neither. False where the token was already
    /// spent: the race is settled here.
    /// </summary>
    public Task<bool> RotateRefreshToken(Guid refreshTokenId, RefreshToken replacement, DateTime now, CancellationToken token);

    public Task RevokeSession(Guid authSessionId, DateTime now, CancellationToken token);

    public Task RevokeAll(Guid userId, DateTime now, CancellationToken token);
}

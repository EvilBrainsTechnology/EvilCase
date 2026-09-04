using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IRefreshTokenStore
{
    public Task AddRefreshToken(RefreshToken refreshToken, CancellationToken token);

    public Task<RefreshToken?> FindRefreshToken(string tokenHash, CancellationToken token);

    /// <summary>
    /// False where the token was already spent: the race is settled here.
    /// </summary>
    public Task<bool> RevokeRefreshToken(Guid refreshTokenId, DateTime now, CancellationToken token);

    public Task RevokeSession(Guid authSessionId, DateTime now, CancellationToken token);

    public Task RevokeAll(Guid userId, DateTime now, CancellationToken token);

    /// <summary>
    /// One row per live session.
    /// </summary>
    public Task<IReadOnlyList<RefreshToken>> GetActive(Guid userId, DateTime now, CancellationToken token);

    /// <summary>
    /// Keyed by AuthSessionId; the chain's oldest row is the sign-in.
    /// </summary>
    public Task<IReadOnlyDictionary<Guid, DateTime>> GetSessionStarts(Guid userId, CancellationToken token);
}

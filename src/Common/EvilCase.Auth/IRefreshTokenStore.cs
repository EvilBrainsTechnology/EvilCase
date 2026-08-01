using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IRefreshTokenStore
{
    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    public Task<RefreshToken?> FindAsync(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Marks one token as consumed. Rotation and nothing else calls this.
    /// </summary>
    public Task RevokeAsync(long id, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Ends one rotation chain: a sign-out, or a replayed token taking its session down.
    /// </summary>
    public Task RevokeSessionAsync(Guid sessionId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session of one user.
    /// </summary>
    public Task RevokeAllAsync(long userId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// The newest token of every chain that is still usable — one row per live session.
    /// </summary>
    public Task<IReadOnlyList<RefreshToken>> GetActiveAsync(long userId, DateTime now, CancellationToken cancellationToken);
}

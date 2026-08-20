using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Auth;

internal interface IRefreshTokenStore
{
    public Task Add(RefreshToken refreshToken, CancellationToken cancellationToken);

    public Task<RefreshToken?> Find(string tokenHash, CancellationToken cancellationToken);

    /// <summary>
    /// Marks one token as consumed. Rotation and nothing else calls this. False where the token was
    /// already spent, which is how two callers presenting the same one are told apart.
    /// </summary>
    public Task<bool> Revoke(Guid id, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Ends one rotation chain: a sign-out, or a replayed token taking its session down.
    /// </summary>
    public Task RevokeSession(Guid authSessionId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// Ends every session of one user.
    /// </summary>
    public Task RevokeAll(Guid userId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// The newest token of every chain that is still usable — one row per live session.
    /// </summary>
    public Task<IReadOnlyList<RefreshToken>> GetActive(Guid userId, DateTime now, CancellationToken cancellationToken);

    /// <summary>
    /// When each of a user's chains began, keyed by session. Its oldest row is the sign-in; the live one
    /// only knows when it was last renewed, because rotation writes a new row every time.
    /// </summary>
    public Task<IReadOnlyDictionary<Guid, DateTime>> GetSessionStarts(Guid userId, CancellationToken cancellationToken);
}

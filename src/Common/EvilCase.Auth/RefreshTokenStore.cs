using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

internal sealed class RefreshTokenStore(IDbContextAccessor accessor) : IRefreshTokenStore
{
    public async Task Add(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        accessor.Current.Add(refreshToken);
        await accessor.Current.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> Find(string tokenHash, CancellationToken cancellationToken) =>
        await accessor.Current.Set<RefreshToken>().SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    // The RevokedAt filter is the whole of the concurrency control: the statement is atomic, so of two
    // callers spending the same token exactly one sees a row change.
    public async Task<bool> Revoke(Guid id, DateTime now, CancellationToken cancellationToken) =>
        await accessor.Current.Set<RefreshToken>()
            .Where(token => token.Id == id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.LastUsed, now)
                    .SetProperty(token => token.Updated, now),
                cancellationToken) > 0;

    public async Task RevokeSession(Guid authSessionId, DateTime now, CancellationToken cancellationToken)
    {
        _ = await accessor.Current.Set<RefreshToken>()
            .Where(token => token.AuthSessionId == authSessionId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.Updated, now),
                cancellationToken);
    }

    public async Task RevokeAll(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        _ = await accessor.Current.Set<RefreshToken>()
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.Updated, now),
                cancellationToken);
    }

    // Rotation revokes as it goes, so at most one row per chain is left unrevoked and the filter alone
    // gives one row per live session.
    public async Task<IReadOnlyList<RefreshToken>> GetActive(Guid userId, DateTime now, CancellationToken cancellationToken) =>
        await accessor.Current.Set<RefreshToken>()
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.Expires > now && token.SessionExpires > now)
            .OrderByDescending(token => token.Created)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, DateTime>> GetSessionStarts(Guid userId, CancellationToken cancellationToken) =>
        await accessor.Current.Set<RefreshToken>()
            .Where(token => token.UserId == userId)
            .GroupBy(token => token.AuthSessionId)
            .Select(chain => new { AuthSessionId = chain.Key, Started = chain.Min(token => token.Created) })
            .ToDictionaryAsync(chain => chain.AuthSessionId, chain => chain.Started, cancellationToken);
}

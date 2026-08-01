using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

internal sealed class RefreshTokenStore(ApplicationDbContext dbContext) : IRefreshTokenStore
{
    public async Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        await dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<RefreshToken?> FindAsync(string tokenHash, CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, cancellationToken);

    public async Task RevokeAsync(long id, DateTime now, CancellationToken cancellationToken)
    {
        _ = await dbContext.RefreshTokens
            .Where(token => token.Id == id && token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.RevokedAt, now)
                    .SetProperty(token => token.LastUsed, now),
                cancellationToken);
    }

    public async Task RevokeSessionAsync(Guid sessionId, DateTime now, CancellationToken cancellationToken)
    {
        _ = await dbContext.RefreshTokens
            .Where(token => token.SessionId == sessionId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    public async Task RevokeAllAsync(long userId, DateTime now, CancellationToken cancellationToken)
    {
        _ = await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(token => token.RevokedAt, now), cancellationToken);
    }

    // Rotation revokes as it goes, so at most one row per chain is left unrevoked and the filter alone
    // gives one row per live session.
    public async Task<IReadOnlyList<RefreshToken>> GetActiveAsync(long userId, DateTime now, CancellationToken cancellationToken) =>
        await dbContext.RefreshTokens
            .Where(token => token.UserId == userId && token.RevokedAt == null && token.Expires > now && token.SessionExpires > now)
            .OrderByDescending(token => token.Created)
            .ToListAsync(cancellationToken);
}

using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

internal sealed class RefreshTokenStore(IDbSession dbSession) : IRefreshTokenStore
{
    public async Task AddRefreshToken(RefreshToken refreshToken, CancellationToken token)
    {
        dbSession.Current.RefreshTokens.Add(refreshToken);
        await dbSession.Current.SaveChangesAsync(token);
    }

    public async Task<RefreshToken?> FindRefreshToken(string tokenHash, CancellationToken token)
    {
        return await dbSession.Current.RefreshTokens.SingleOrDefaultAsync(token => token.TokenHash == tokenHash, token);
    }

    // The RevokedAt filter is the whole of the concurrency control: the update's row lock holds to the
    // commit, and a second caller spending the same token re-evaluates the filter after it and changes no row.
    public async Task<bool> RotateRefreshToken(Guid refreshTokenId, RefreshToken replacement, DateTime now, CancellationToken token)
    {
        var context = dbSession.Current;

        await using var transaction = await dbSession.BeginTransaction(token);

        var spent = await context.RefreshTokens
            .Where(token => token.Id == refreshTokenId)
            .Where(static token => token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(static token => token.RevokedAt, now)
                    .SetProperty(static token => token.LastUsed, now),
                token);

        if (spent == 0)
            return false;

        context.RefreshTokens.Add(replacement);
        await context.SaveChangesAsync(token);

        await transaction.CommitAsync(token);

        return true;
    }

    public async Task RevokeSession(Guid authSessionId, DateTime now, CancellationToken token)
    {
        await dbSession.Current.RefreshTokens
            .Where(token => token.AuthSessionId == authSessionId)
            .Where(static token => token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(static token => token.RevokedAt, now),
                token);
    }

    public async Task RevokeAll(Guid userId, DateTime now, CancellationToken token)
    {
        await dbSession.Current.RefreshTokens
            .Where(token => token.UserId == userId)
            .Where(static token => token.RevokedAt == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(static token => token.RevokedAt, now),
                token);
    }
}

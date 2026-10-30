using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Auth;

internal sealed class ApiTokenStore(IDbSession dbSession) : IApiTokenStore
{
    public async Task<bool> AddApiToken(ApiToken apiToken, DateTime now, int maxActive, CancellationToken token)
    {
        var context = dbSession.Current;

        await using var transaction = await dbSession.BeginTransaction(token);

        // The user row's lock settles two creations at once: the second counts after the first has written.
        await context.Users
            .FromSql($"""SELECT * FROM "Users" WHERE "Id" = {apiToken.UserId} FOR UPDATE""")
            .IgnoreQueryFilters()
            .Select(static user => user.Id)
            .SingleAsync(token);

        var active = await context.ApiTokens
            .Where(stored => stored.UserId == apiToken.UserId)
            .Where(stored => stored.Expires > now)
            .CountAsync(token);

        if (active >= maxActive)
            return false;

        context.ApiTokens.Add(apiToken);
        await context.SaveChangesAsync(token);

        await transaction.CommitAsync(token);

        return true;
    }

    public async Task<IReadOnlyList<ApiToken>> ListApiTokens(Guid userId, CancellationToken token)
    {
        return await dbSession.Current.ApiTokens
            .Where(stored => stored.UserId == userId)
            .OrderByDescending(static stored => stored.Created)
            .ToListAsync(token);
    }

    // The user carries the tenant filter, and no tenant is known yet.
    public async Task<ApiToken?> FindApiToken(string tokenHash, CancellationToken token)
    {
        return await dbSession.Current.ApiTokens
            .IgnoreQueryFilters()
            .Include(static stored => stored.User)
            .SingleOrDefaultAsync(stored => stored.TokenHash == tokenHash, token);
    }

    public async Task<bool> DeleteApiToken(Guid userId, Guid apiTokenId, CancellationToken token)
    {
        return await dbSession.Current.ApiTokens
            .Where(stored => stored.Id == apiTokenId)
            .Where(stored => stored.UserId == userId)
            .ExecuteDeleteAsync(token) > 0;
    }

    // The condition sits in the statement, so concurrent requests write at most once per window.
    public async Task TouchApiToken(Guid apiTokenId, DateTime now, DateTime staleBefore, CancellationToken token)
    {
        await dbSession.Current.ApiTokens
            .Where(stored => stored.Id == apiTokenId)
            .Where(stored => stored.LastUsed == null || stored.LastUsed < staleBefore)
            .ExecuteUpdateAsync(setters => setters.SetProperty(static stored => stored.LastUsed, now), token);
    }
}

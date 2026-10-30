using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.ApiTokens;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class FakeApiTokenStore : IApiTokenStore
{
    private readonly Lock writes = new();

    private readonly List<ApiToken> tokens = [];

    /// <summary>
    /// Returns the secret, which only the caller of a real creation would see.
    /// </summary>
    public string Issue(User user, ApiTokenScope scope, DateTime? expires = null)
    {
        var secret = ApiTokenValue.Create();

        lock (this.writes)
        {
            this.tokens.Add(new ApiToken
            {
                UserId = user.Id,
                User = user,
                Name = $"token-{this.tokens.Count}",
                Scope = scope,
                TokenHash = ApiTokenValue.Hash(secret),
                Expires = expires ?? DateTime.UtcNow.AddDays(90),
            });
        }

        return secret;
    }

    public void Forget(string secret)
    {
        var hash = ApiTokenValue.Hash(secret);

        lock (this.writes)
            this.tokens.RemoveAll(stored => string.Equals(stored.TokenHash, hash, StringComparison.Ordinal));
    }

    public async Task<bool> AddApiToken(ApiToken apiToken, DateTime now, int maxActive, CancellationToken token)
    {
        lock (this.writes)
            this.tokens.Add(apiToken with { Created = now });

        return true;
    }

    public async Task<IReadOnlyList<ApiToken>> ListApiTokens(Guid userId, CancellationToken token)
    {
        lock (this.writes)
            return [.. this.tokens.Where(stored => stored.UserId == userId)];
    }

    public async Task<ApiToken?> FindApiToken(string tokenHash, CancellationToken token)
    {
        lock (this.writes)
            return this.tokens.Find(stored => string.Equals(stored.TokenHash, tokenHash, StringComparison.Ordinal));
    }

    public async Task<bool> DeleteApiToken(Guid userId, Guid apiTokenId, CancellationToken token)
    {
        lock (this.writes)
            return this.tokens.RemoveAll(stored => stored.Id == apiTokenId && stored.UserId == userId) > 0;
    }

    public Task TouchApiToken(Guid apiTokenId, DateTime now, DateTime staleBefore, CancellationToken token)
    {
        return Task.CompletedTask;
    }
}

using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly List<RefreshToken> tokens = [];

    public IReadOnlyList<RefreshToken> All => this.tokens;

    public RefreshToken Find(string tokenHash) =>
        this.tokens.Single(token => string.Equals(token.TokenHash, tokenHash, StringComparison.Ordinal));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        this.tokens.Add(refreshToken with { Id = this.tokens.Count + 1 });

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> FindAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(this.tokens.Find(token => string.Equals(token.TokenHash, tokenHash, StringComparison.Ordinal)));

    public Task RevokeAsync(long id, DateTime now, CancellationToken cancellationToken)
    {
        this.Revoke(token => token.Id == id, now, alsoUsed: true);

        return Task.CompletedTask;
    }

    public Task RevokeSessionAsync(Guid sessionId, DateTime now, CancellationToken cancellationToken)
    {
        this.Revoke(token => token.SessionId == sessionId, now, alsoUsed: false);

        return Task.CompletedTask;
    }

    public Task RevokeAllAsync(long userId, DateTime now, CancellationToken cancellationToken)
    {
        this.Revoke(token => token.UserId == userId, now, alsoUsed: false);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RefreshToken>> GetActiveAsync(long userId, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(
            [.. this.tokens.Where(token => token.UserId == userId && token.RevokedAt is null && token.Expires > now && token.SessionExpires > now)]);

    private void Revoke(Func<RefreshToken, bool> match, in DateTime now, bool alsoUsed)
    {
        for (var i = 0; i < this.tokens.Count; i++)
        {
            if (this.tokens[i].RevokedAt is null && match(this.tokens[i]))
                this.tokens[i] = this.tokens[i] with { RevokedAt = now, LastUsed = alsoUsed ? now : this.tokens[i].LastUsed };
        }
    }
}

using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly List<RefreshToken> tokens = [];

    private TaskCompletionSource? gate;

    public IReadOnlyList<RefreshToken> All => this.tokens;

    /// <summary>
    /// Holds every caller at the moment it would spend a token, so a test can line two of them up behind
    /// the read that found it live. Continuations run inline, so <see cref="Resume"/> lets them through
    /// one after the other rather than actually in parallel — the race being modelled is the interleaving,
    /// not the threading.
    /// </summary>
    public void PauseBeforeRevoking() => this.gate = new();

    public void Resume() => this.gate?.SetResult();

    public RefreshToken Find(string tokenHash) =>
        this.tokens.Single(token => string.Equals(token.TokenHash, tokenHash, StringComparison.Ordinal));

    public Task AddAsync(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        this.tokens.Add(refreshToken with { Id = this.tokens.Count + 1 });

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> FindAsync(string tokenHash, CancellationToken cancellationToken) =>
        Task.FromResult(this.tokens.Find(token => string.Equals(token.TokenHash, tokenHash, StringComparison.Ordinal)));

    public async Task<bool> RevokeAsync(long id, DateTime now, CancellationToken cancellationToken)
    {
        if (this.gate is { } paused)
            await paused.Task;

        return this.Revoke(token => token.Id == id, now, alsoUsed: true) > 0;
    }

    public Task RevokeSessionAsync(Guid sessionId, DateTime now, CancellationToken cancellationToken)
    {
        _ = this.Revoke(token => token.SessionId == sessionId, now, alsoUsed: false);

        return Task.CompletedTask;
    }

    public Task RevokeAllAsync(long userId, DateTime now, CancellationToken cancellationToken)
    {
        _ = this.Revoke(token => token.UserId == userId, now, alsoUsed: false);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RefreshToken>> GetActiveAsync(long userId, DateTime now, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<RefreshToken>>(
            [.. this.tokens.Where(token => token.UserId == userId && token.RevokedAt is null && token.Expires > now && token.SessionExpires > now)]);

    private int Revoke(Func<RefreshToken, bool> match, in DateTime now, bool alsoUsed)
    {
        var revoked = 0;

        for (var i = 0; i < this.tokens.Count; i++)
        {
            if (this.tokens[i].RevokedAt is null && match(this.tokens[i]))
            {
                this.tokens[i] = this.tokens[i] with { RevokedAt = now, LastUsed = alsoUsed ? now : this.tokens[i].LastUsed };
                revoked++;
            }
        }

        return revoked;
    }
}

using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Auth;

internal sealed class FakeRefreshTokenStore : IRefreshTokenStore
{
    private readonly List<RefreshToken> tokens = [];

    // Every write goes through this. Only the paused test below releases two callers at once, and the
    // scheduler is free to run them in parallel — a torn list would fail as a race the store does not have.
    private readonly Lock writes = new();

    private TaskCompletionSource? gate;

    public IReadOnlyList<RefreshToken> All => this.tokens;

    /// <summary>
    /// Holds every caller at the moment it would spend a token, so a test can line two of them up behind
    /// the read that found it live — which is the interleaving the filter on that write has to settle.
    /// </summary>
    public void PauseBeforeRevoking()
    {
        this.gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Resume()
    {
        this.gate?.SetResult();
    }

    public Task Add(RefreshToken refreshToken, CancellationToken cancellationToken)
    {
        lock (this.writes)
            this.tokens.Add(refreshToken);

        return Task.CompletedTask;
    }

    public Task<RefreshToken?> Find(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(this.tokens.Find(token => string.Equals(token.TokenHash, tokenHash, StringComparison.Ordinal)));
    }

    public async Task<bool> Revoke(Guid id, DateTime now, CancellationToken cancellationToken)
    {
        if (this.gate is { } paused)
            await paused.Task;

        return this.RevokeMatching(token => token.Id == id, now, alsoUsed: true) > 0;
    }

    public Task RevokeSession(Guid authSessionId, DateTime now, CancellationToken cancellationToken)
    {
        this.RevokeMatching(token => token.AuthSessionId == authSessionId, now, alsoUsed: false);

        return Task.CompletedTask;
    }

    public Task RevokeAll(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        this.RevokeMatching(token => token.UserId == userId, now, alsoUsed: false);

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<RefreshToken>> GetActive(Guid userId, DateTime now, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<RefreshToken>>(
            [.. this.tokens.Where(token => token.UserId == userId && token.RevokedAt is null && token.Expires > now && token.SessionExpires > now)]);
    }

    public Task<IReadOnlyDictionary<Guid, DateTime>> GetSessionStarts(Guid userId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyDictionary<Guid, DateTime>>(
            this.tokens
                .Where(token => token.UserId == userId)
                .GroupBy(token => token.AuthSessionId)
                .ToDictionary(chain => chain.Key, chain => chain.Min(token => token.Created)));
    }

    private int RevokeMatching(Func<RefreshToken, bool> match, in DateTime now, bool alsoUsed)
    {
        var revoked = 0;

        lock (this.writes)
        {
            for (var i = 0; i < this.tokens.Count; i++)
            {
                if (this.tokens[i].RevokedAt is null && match(this.tokens[i]))
                {
                    this.tokens[i] = this.tokens[i] with { RevokedAt = now, LastUsed = alsoUsed ? now : this.tokens[i].LastUsed };
                    revoked++;
                }
            }
        }

        return revoked;
    }
}

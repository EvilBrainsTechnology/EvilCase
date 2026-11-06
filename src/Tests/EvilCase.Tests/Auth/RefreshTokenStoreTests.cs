using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Auth;

public class RefreshTokenStoreTests : TenantFixture
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task OnlyOneOfTwoConcurrentRotationsSpendsTheToken()
    {
        var spent = await this.AddLiveRefreshToken();

        await using var otherContext = TestDatabase.CreateMigrated(this.Tenant.UserContext);
        var store = new RefreshTokenStore(new TransactionalDbSession(this.Tenant.Context));
        var other = new RefreshTokenStore(new TransactionalDbSession(otherContext));

        var replacements = new RefreshToken[]
        {
            this.NewRefreshToken(spent.AuthSessionId, RefreshTokenValue.Hash(RefreshTokenValue.Create())),
            this.NewRefreshToken(spent.AuthSessionId, RefreshTokenValue.Hash(RefreshTokenValue.Create())),
        };

        var results = await Task.WhenAll(
            store.RotateRefreshToken(spent.Id, replacements[0], Now, CancellationToken.None),
            other.RotateRefreshToken(spent.Id, replacements[1], Now, CancellationToken.None));

        var replacementIds = replacements.Select(static replacement => replacement.Id).ToList();
        var stored = await this.Tenant.Context.RefreshTokens
            .AsNoTracking()
            .CountAsync(token => replacementIds.Contains(token.Id));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results, Is.EquivalentTo([true, false]), "the second rotation finds the token spent once the first commits");
            Assert.That(stored, Is.EqualTo(1), "only the rotation that spent the token stores its replacement");
        }
    }

    [Test]
    public async Task AFailedWriteOfTheReplacementLeavesTheSpentTokenLive()
    {
        var spent = await this.AddLiveRefreshToken();

        var store = new RefreshTokenStore(new TransactionalDbSession(this.Tenant.Context));
        var clashing = this.NewRefreshToken(spent.AuthSessionId, spent.TokenHash);

        await Assert.ThrowsAsync<DbUpdateException>(async () => await store.RotateRefreshToken(spent.Id, clashing, Now, CancellationToken.None));

        var revokedAt = await this.Tenant.Context.RefreshTokens
            .AsNoTracking()
            .Where(stored => stored.Id == spent.Id)
            .Select(static stored => stored.RevokedAt)
            .SingleAsync();

        Assert.That(revokedAt, Is.Null, "the token is spent only together with the write of its replacement");
    }

    private async Task<RefreshToken> AddLiveRefreshToken()
    {
        var refreshToken = this.NewRefreshToken(Guid.CreateVersion7(), RefreshTokenValue.Hash(RefreshTokenValue.Create()));

        this.Tenant.Context.RefreshTokens.Add(refreshToken);
        await this.Tenant.Context.SaveChangesAsync();

        return refreshToken;
    }

    private RefreshToken NewRefreshToken(Guid authSessionId, string tokenHash)
    {
        return new()
        {
            UserId = this.Tenant.UserId,
            AuthSessionId = authSessionId,
            TokenHash = tokenHash,
            Expires = Now.AddDays(30),
            SessionExpires = Now.AddDays(90),
        };
    }
}

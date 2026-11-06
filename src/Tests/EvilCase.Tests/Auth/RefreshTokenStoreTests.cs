using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Auth;

public class RefreshTokenStoreTests : TenantFixture
{
    private static readonly DateTime Now = new(2026, 11, 6, 12, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task AFailedWriteOfTheReplacementLeavesTheSpentTokenLive()
    {
        var spent = this.NewRefreshToken(Guid.CreateVersion7(), RefreshTokenValue.Hash(RefreshTokenValue.Create()));

        this.Tenant.Context.RefreshTokens.Add(spent);
        await this.Tenant.Context.SaveChangesAsync();

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

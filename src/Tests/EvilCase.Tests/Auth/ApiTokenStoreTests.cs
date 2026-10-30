using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.ApiTokens;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Auth;

public class ApiTokenStoreTests : TenantFixture
{
    private static readonly DateTime Now = new(2026, 10, 30, 12, 0, 0, DateTimeKind.Utc);

    private static readonly TimeSpan Resolution = TimeSpan.FromMinutes(1);

    [Test]
    public async Task TheLastUseIsStampedAtMostOnceAMinute()
    {
        var store = new ApiTokenStore(new FixedDbSession(this.Tenant.Context));
        var apiToken = await this.AddApiToken(this.Tenant.UserId);

        await Touch(store, apiToken, Now);
        await Touch(store, apiToken, Now.AddSeconds(59));
        var withinTheMinute = await this.LastUsed(apiToken);

        await Touch(store, apiToken, Now.AddSeconds(61));
        var afterTheMinute = await this.LastUsed(apiToken);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withinTheMinute, Is.EqualTo(Now), "a use within a minute of the stamped one writes nothing");
            Assert.That(afterTheMinute, Is.EqualTo(Now.AddSeconds(61)), "a use a minute after the stamped one moves it");
        }
    }

    [Test]
    public async Task ATokenIsFoundWithItsUserBeforeATenantIsKnown()
    {
        var apiToken = await this.AddApiToken(this.Tenant.UserId);

        await using var anonymous = TestDatabase.CreateMigrated(new StubUserContext());
        var store = new ApiTokenStore(new FixedDbSession(anonymous));

        var found = await store.FindApiToken(apiToken.TokenHash, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found?.Id, Is.EqualTo(apiToken.Id), "authentication runs before any tenant is known");
            Assert.That(found?.User?.Id, Is.EqualTo(this.Tenant.UserId), "the tenant comes from the user row, so the user is read with the token");
        }
    }

    [Test]
    public async Task AUserRevokesOnlyTheirOwnToken()
    {
        var other = await this.Tenant.AddUser();
        var apiToken = await this.AddApiToken(this.Tenant.UserId);
        var store = new ApiTokenStore(new FixedDbSession(this.Tenant.Context));

        var byOther = await store.DeleteApiToken(other.Id, apiToken.Id, CancellationToken.None);
        var stillThere = await this.Tenant.Context.ApiTokens.AnyAsync(stored => stored.Id == apiToken.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byOther, Is.False);
            Assert.That(stillThere, Is.True, "another user of the same tenant cannot revoke the token");
        }
    }

    private static async Task Touch(ApiTokenStore store, ApiToken apiToken, DateTime now)
    {
        await store.TouchApiToken(apiToken.Id, now, now - Resolution, CancellationToken.None);
    }

    private async Task<ApiToken> AddApiToken(Guid userId)
    {
        var apiToken = new ApiToken
        {
            UserId = userId,
            Name = "agent",
            Scope = ApiTokenScope.ReadOnly,
            TokenHash = ApiTokenValue.Hash(ApiTokenValue.Create()),
            Expires = Now.AddDays(90),
        };

        this.Tenant.Context.ApiTokens.Add(apiToken);
        await this.Tenant.Context.SaveChangesAsync();

        return apiToken;
    }

    private async Task<DateTime?> LastUsed(ApiToken apiToken)
    {
        return await this.Tenant.Context.ApiTokens
            .AsNoTracking()
            .Where(stored => stored.Id == apiToken.Id)
            .Select(static stored => stored.LastUsed)
            .SingleAsync();
    }
}

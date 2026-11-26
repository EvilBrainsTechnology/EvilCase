using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Api.Contract.ApiTokens;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.ApiTokens;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Auth;

public class ApiTokenServiceTests : TenantFixture
{
    private static readonly ApiTokenCreateRequest Request = new() { Name = "  Claude Code  ", Scope = ApiTokenScope.ReadWrite, ExpiresInDays = 30 };

    private ApiTokenService service = null!;

    [SetUp]
    public void SetUpService()
    {
        var store = new ApiTokenStore(new TransactionalDbSession(this.Tenant.Context));

        this.service = new ApiTokenService(store, TimeProvider.System, NullLogger<ApiTokenService>.Instance);
    }

    [Test]
    public async Task TheSecretTravelsOnceAndOnlyItsHashIsStored()
    {
        var created = UnionAssert.Holds<ApiTokenCreated>(await this.service.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None));
        var secret = created.Secret;

        var stored = await this.Tenant.Context.ApiTokens
            .AsNoTracking()
            .SingleAsync(apiToken => apiToken.Id == created.Token.ApiTokenId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(secret, Does.Match("^evc_[A-Za-z0-9_-]{43}$"), "a fixed prefix and 256 random bits, base64url");
            Assert.That(stored.TokenHash, Is.EqualTo(Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)))));
            Assert.That(stored.Name, Is.EqualTo("Claude Code"));
            Assert.That(stored.Expires, Is.EqualTo(DateTime.UtcNow.AddDays(30)).Within(TimeSpan.FromMinutes(1)));
        }
    }

    [Test]
    public async Task TheTokenPastTheLimitIsRefused()
    {
        for (var index = 0; index < ApiTokenLimits.MaxActive; index++)
            await this.service.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None);

        var past = await this.service.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None);
        var count = await this.Tenant.Context.ApiTokens.CountAsync(apiToken => apiToken.UserId == this.Tenant.UserId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(past.Value, Is.EqualTo(ApiTokenCreateFailure.LimitReached));
            Assert.That(count, Is.EqualTo(ApiTokenLimits.MaxActive), "the refused token writes nothing");
        }
    }

    [Test]
    public async Task AnExpiredTokenLeavesRoomUnderTheLimit()
    {
        for (var index = 0; index < ApiTokenLimits.MaxActive; index++)
        {
            this.Tenant.Context.ApiTokens.Add(new ApiToken
            {
                UserId = this.Tenant.UserId,
                Name = "expired",
                Scope = ApiTokenScope.ReadOnly,
                TokenHash = ApiTokenValue.Hash(ApiTokenValue.Create()),
                Expires = DateTime.UtcNow.AddMinutes(-1),
            });
        }

        await this.Tenant.Context.SaveChangesAsync();

        var result = await this.service.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None);

        Assert.That(result.Value, Is.TypeOf<ApiTokenCreated>(), "the limit counts unexpired tokens only");
    }

    [Test]
    public async Task ConcurrentCreationsStopAtTheLimit()
    {
        for (var index = 0; index < ApiTokenLimits.MaxActive - 1; index++)
            await this.service.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None);

        await using var otherContext = TestDatabase.CreateMigrated(this.Tenant.UserContext);
        var other = new ApiTokenService(new ApiTokenStore(new TransactionalDbSession(otherContext)), TimeProvider.System, NullLogger<ApiTokenService>.Instance);

        var results = await Task.WhenAll(
            this.service.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None),
            other.CreateApiToken(this.Tenant.UserId, Request, CancellationToken.None));

        var count = await this.Tenant.Context.ApiTokens.CountAsync(apiToken => apiToken.UserId == this.Tenant.UserId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(results.Count(static result => result.Value is ApiTokenCreated), Is.EqualTo(1));
            Assert.That(results.Select(static result => result.Value), Does.Contain(ApiTokenCreateFailure.LimitReached));
            Assert.That(count, Is.EqualTo(ApiTokenLimits.MaxActive), "the second creation counts after the first has written");
        }
    }
}

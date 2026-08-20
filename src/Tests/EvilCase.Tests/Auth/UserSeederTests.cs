using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// Registration is closed, so this is the only way a deployment gets its first account — and the one
/// thing that must never quietly reinstate an account somebody removed.
/// </summary>
public class UserSeederTests
{
    private const string SeedEmail = "Admin@EvilCase.Test";

    private const string SeedPassword = "seeded-administrator";

    [Test]
    public async Task AnEmptyDatabaseGetsTheConfiguredAdministrator()
    {
        var store = new FakeUserStore();

        await Seeder(store, SeedEmail, SeedPassword).Seed(CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(store.Account, Is.Not.Null);
            Assert.That(store.Tenant, Is.Not.Null);
            Assert.That(store.DefaultContact, Is.Not.Null);
        }

        var user = store.Get(store.DefaultContact!.UserId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Email, Is.EqualTo("admin@evilcase.test"), "the seed goes through the same normalisation as a sign-in");
            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
            Assert.That(PasswordHasher.Verify(SeedPassword, user.PasswordHash), Is.True);
            Assert.That(user.TenantId, Is.EqualTo(store.Tenant!.Id));
            Assert.That(store.Tenant!.AccountId, Is.EqualTo(store.Account!.Id));
            Assert.That(store.DefaultContact!.TenantId, Is.EqualTo(user.TenantId));
            Assert.That(store.DefaultContact!.UserId, Is.EqualTo(user.Id));
            Assert.That(user.DefaultContactId, Is.EqualTo(store.DefaultContact!.Id), "a user without a default contact has nothing to prefill an act with");
        }
    }

    [Test]
    public async Task NothingHappensOnceAnyUserExists()
    {
        var store = new FakeUserStore();

        _ = store.Seed(new()
        {
            TenantId = Guid.CreateVersion7(),
            Email = "someone@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.User,
        });

        await Seeder(store, SeedEmail, SeedPassword).Seed(CancellationToken.None);

        Assert.That(store.Account, Is.Null);
    }

    [Test]
    public async Task AnEnvironmentThatNamesNoCredentialsGetsNoAccount()
    {
        var store = new FakeUserStore();

        await Seeder(store, email: null, password: null).Seed(CancellationToken.None);

        Assert.That(await store.Any(CancellationToken.None), Is.False);
    }

    /// <summary>
    /// Half a seed is a misconfiguration, not an instruction to invent the other half.
    /// </summary>
    [Test]
    public async Task AnEmailWithoutAPasswordCreatesNothing()
    {
        var store = new FakeUserStore();

        await Seeder(store, SeedEmail, password: null).Seed(CancellationToken.None);

        Assert.That(await store.Any(CancellationToken.None), Is.False);
    }

    private static UserSeeder Seeder(FakeUserStore store, string? email, string? password)
    {
        var settings = AuthTestHarness.CreateSettings() with { Seed = new() { Email = email, Password = password } };

        return new UserSeeder(store, Options.Create(settings), NullLogger<UserSeeder>.Instance);
    }
}

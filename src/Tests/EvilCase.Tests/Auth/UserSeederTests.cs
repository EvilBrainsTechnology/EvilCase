using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Data;
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
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);

        await Seeder(context, userContext, store, SeedEmail, SeedPassword).SeedUser(CancellationToken.None);

        var account = context.Added<Account>().Single();
        var tenant = context.Added<Tenant>().Single();
        var user = store.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Email, Is.EqualTo("admin@evilcase.test"), "the seed goes through the same normalisation as a sign-in");
            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
            Assert.That(PasswordHasher.Verify(SeedPassword, user.PasswordHash), Is.True);
            Assert.That(tenant.AccountId, Is.EqualTo(account.Id));
        }
    }

    [Test]
    public async Task TheFirstAdministratorGetsAnAccountAndATenant()
    {
        var store = new FakeUserStore();
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);

        await Seeder(context, userContext, store, SeedEmail, SeedPassword).SeedUser(CancellationToken.None);

        var tenant = context.Added<Tenant>().Single();
        var user = store.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Added<Account>(), Has.Exactly(1).Items);
            Assert.That(context.Added<Tenant>(), Has.Exactly(1).Items);
            Assert.That(user.TenantId, Is.EqualTo(Guid.Empty), "the seed names no tenant on the administrator; the write is what stamps it");
            Assert.That(userContext.Entered, Is.EqualTo([(tenant.Id, user.Id)]), "the user is written under the tenant the seed created, which is what stamps it");
        }
    }

    [Test]
    public async Task NothingHappensOnceAnyUserExists()
    {
        var store = new FakeUserStore();
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);

        store.SeedUser(new()
        {
            TenantId = Guid.CreateVersion7(),
            Email = "someone@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.User,
        });

        await Seeder(context, userContext, store, SeedEmail, SeedPassword).SeedUser(CancellationToken.None);

        Assert.That(context.Added<Account>(), Is.Empty);
    }

    [Test]
    public async Task AnEnvironmentThatNamesNoCredentialsGetsNoAccount()
    {
        var store = new FakeUserStore();
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);

        await Seeder(context, userContext, store, email: null, password: null).SeedUser(CancellationToken.None);

        Assert.That(await store.Any(CancellationToken.None), Is.False);
    }

    /// <summary>
    /// Half a seed is a misconfiguration, not an instruction to invent the other half.
    /// </summary>
    [Test]
    public async Task AnEmailWithoutAPasswordCreatesNothing()
    {
        var store = new FakeUserStore();
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);

        await Seeder(context, userContext, store, SeedEmail, password: null).SeedUser(CancellationToken.None);

        Assert.That(await store.Any(CancellationToken.None), Is.False);
    }

    /// <summary>
    /// The transaction the host opens around the seed hangs on this, so a host with nothing to seed
    /// starts without reaching the database at all.
    /// </summary>
    [TestCase(SeedEmail, SeedPassword, ExpectedResult = true)]
    [TestCase(SeedEmail, null, ExpectedResult = false)]
    [TestCase(null, SeedPassword, ExpectedResult = false)]
    [TestCase(null, null, ExpectedResult = false)]
    public bool OnlyBothHalvesOfTheSeedCountAsConfigured(string? email, string? password)
    {
        var userContext = new StubUserContext();

        return Seeder(FakeApplicationDbContext.Create(userContext), userContext, new FakeUserStore(), email, password).IsConfigured;
    }

    private static UserSeeder Seeder(FakeApplicationDbContext context, StubUserContext userContext, FakeUserStore store, string? email, string? password)
    {
        var settings = AuthTestHarness.CreateSettings() with { Seed = new() { Email = email, Password = password } };

        return new UserSeeder(new FixedDbSession(context), store, userContext, Options.Create(settings), NullLogger<UserSeeder>.Instance);
    }
}

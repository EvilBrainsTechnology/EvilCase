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
        var session = new FakeDbSession();

        await Seeder(session, store, SeedEmail, SeedPassword).Seed(CancellationToken.None);

        var account = session.Added.OfType<Account>().Single();
        var tenant = session.Added.OfType<Tenant>().Single();
        var contact = session.Added.OfType<Contact>().Single();
        var user = store.Get(contact.UserId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Email, Is.EqualTo("admin@evilcase.test"), "the seed goes through the same normalisation as a sign-in");
            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
            Assert.That(PasswordHasher.Verify(SeedPassword, user.PasswordHash), Is.True);
            Assert.That(user.TenantId, Is.EqualTo(tenant.Id));
            Assert.That(tenant.AccountId, Is.EqualTo(account.Id));
            Assert.That(contact.TenantId, Is.EqualTo(user.TenantId));
            Assert.That(contact.UserId, Is.EqualTo(user.Id));
            Assert.That(user.DefaultContactId, Is.EqualTo(contact.Id), "a user without a default contact has nothing to prefill an act with");
        }
    }

    [Test]
    public async Task TheFirstAdministratorGetsAnAccountATenantAndADefaultContact()
    {
        var store = new FakeUserStore();
        var session = new FakeDbSession();

        await Seeder(session, store, SeedEmail, SeedPassword).Seed(CancellationToken.None);

        var tenant = session.Added.OfType<Tenant>().Single();
        var contact = session.Added.OfType<Contact>().Single();
        var user = store.Get(contact.UserId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(session.Added.OfType<Account>(), Has.Exactly(1).Items);
            Assert.That(session.Added.OfType<Tenant>(), Has.Exactly(1).Items);
            Assert.That(session.Added.OfType<Contact>(), Has.Exactly(1).Items);
            Assert.That(contact.TenantId, Is.EqualTo(tenant.Id), "the default contact belongs to the tenant the seed created");
            Assert.That(store.Get(user.Id).DefaultContactId, Is.EqualTo(contact.Id), "the user points at the contact the seed made for it");
        }
    }

    [Test]
    public async Task NothingHappensOnceAnyUserExists()
    {
        var store = new FakeUserStore();
        var session = new FakeDbSession();

        _ = store.Seed(new()
        {
            TenantId = Guid.CreateVersion7(),
            Email = "someone@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.User,
        });

        await Seeder(session, store, SeedEmail, SeedPassword).Seed(CancellationToken.None);

        Assert.That(session.Added, Is.Empty);
    }

    [Test]
    public async Task AnEnvironmentThatNamesNoCredentialsGetsNoAccount()
    {
        var store = new FakeUserStore();
        var session = new FakeDbSession();

        await Seeder(session, store, email: null, password: null).Seed(CancellationToken.None);

        Assert.That(await store.Any(CancellationToken.None), Is.False);
    }

    /// <summary>
    /// Half a seed is a misconfiguration, not an instruction to invent the other half.
    /// </summary>
    [Test]
    public async Task AnEmailWithoutAPasswordCreatesNothing()
    {
        var store = new FakeUserStore();
        var session = new FakeDbSession();

        await Seeder(session, store, SeedEmail, password: null).Seed(CancellationToken.None);

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
    public bool OnlyBothHalvesOfTheSeedCountAsConfigured(string? email, string? password) =>
        Seeder(new FakeDbSession(), new FakeUserStore(), email, password).IsConfigured;

    private static UserSeeder Seeder(FakeDbSession session, FakeUserStore store, string? email, string? password)
    {
        var settings = AuthTestHarness.CreateSettings() with { Seed = new() { Email = email, Password = password } };

        return new UserSeeder(session, store, new StubTenantContext(), Options.Create(settings), NullLogger<UserSeeder>.Instance);
    }
}

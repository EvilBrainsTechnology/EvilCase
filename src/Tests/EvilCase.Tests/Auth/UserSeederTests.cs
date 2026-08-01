using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Api.Contract.User;
using EvilBrains.EvilCase.Auth;
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

        var user = store.Get(1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Email, Is.EqualTo("admin@evilcase.test"), "the seed goes through the same normalisation as a sign-in");
            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
            Assert.That(PasswordHasher.Verify(SeedPassword, user.PasswordHash), Is.True);
        }
    }

    [Test]
    public async Task NothingHappensOnceAnyUserExists()
    {
        var store = new FakeUserStore();

        _ = store.Seed(new()
        {
            Email = "someone@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.User,
            Created = DateTime.UtcNow,
        });

        await Seeder(store, SeedEmail, SeedPassword).Seed(CancellationToken.None);

        Assert.That(await store.Any(CancellationToken.None) && store.Get(1).Role == UserRole.User, Is.True);
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

        return new UserSeeder(
            store,
            Options.Create(settings),
            new TestTimeProvider(new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc)),
            NullLogger<UserSeeder>.Instance);
    }
}

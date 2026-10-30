using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Auth;

public class UserSeederConfigurationTests
{
    /// <summary>
    /// The transaction the host opens around the seed hangs on this, so a host with nothing to seed
    /// starts without reaching the database at all.
    /// </summary>
    [TestCase("Admin@EvilCase.Test", "seeded-administrator", ExpectedResult = true)]
    [TestCase("Admin@EvilCase.Test", null, ExpectedResult = false)]
    [TestCase(null, "seeded-administrator", ExpectedResult = false)]
    [TestCase(null, null, ExpectedResult = false)]
    public bool OnlyBothHalvesOfTheSeedCountAsConfigured(string? email, string? password)
    {
        var settings = AuthTestHarness.CreateSettings() with { Seed = new() { Email = email, Password = password } };
        var seeder = new UserSeeder(
            Substitute.For<IDbSession>(),
            new FakeUserStore(),
            new StubUserContext(),
            Options.Create(settings),
            NullLogger<UserSeeder>.Instance);

        return seeder.IsConfigured;
    }
}

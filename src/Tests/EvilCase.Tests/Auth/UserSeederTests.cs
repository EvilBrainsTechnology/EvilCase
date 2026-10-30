using EvilBrains.Cryptography;
using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Tests.Auth;

/// <summary>
/// The seed needs a user table no other test writes to, so the fixture runs on a database of its own
/// and every test rolls back what it wrote.
/// </summary>
public class UserSeederTests
{
    private const string SeedEmail = "Admin@EvilCase.Test";

    private const string SeedPassword = "seeded-administrator";

    private readonly string databaseName = $"evilcase_seed_{Guid.NewGuid().ToString("N")[..8]}";

    private string connectionString = null!;

    private StubUserContext userContext = null!;

    private ApplicationDbContext context = null!;

    private IDbContextTransaction transaction = null!;

    [OneTimeSetUp]
    public async Task CreateDatabase()
    {
        this.connectionString = await TestDatabase.CreateScratchDatabase(this.databaseName);

        await using var migrating = this.CreateContext(new StubUserContext());
        await migrating.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task DropDatabase()
    {
        await TestDatabase.DropScratchDatabase(this.databaseName);
    }

    [SetUp]
    public async Task BeginTransaction()
    {
        this.userContext = new StubUserContext();
        this.context = this.CreateContext(this.userContext);
        this.transaction = await this.context.Database.BeginTransactionAsync();
    }

    [TearDown]
    public async Task RollBack()
    {
        await this.transaction.DisposeAsync();
        await this.context.DisposeAsync();
    }

    [Test]
    public async Task AnEmptyDatabaseGetsTheConfiguredAdministratorInATenantOfItsOwn()
    {
        await this.Seeder(SeedEmail, SeedPassword).SeedUser(CancellationToken.None);

        var user = await this.context.Users.IgnoreQueryFilters().SingleAsync();
        var tenant = await this.context.Tenants.IgnoreQueryFilters().SingleAsync();
        var account = await this.context.Accounts.IgnoreQueryFilters().SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(user.Email, Is.EqualTo("admin@evilcase.test"), "the seed goes through the same normalisation as a sign-in");
            Assert.That(user.Role, Is.EqualTo(UserRole.Admin));
            Assert.That(PasswordHasher.Verify(SeedPassword, user.PasswordHash), Is.True);
            Assert.That(user.TenantId, Is.EqualTo(tenant.Id), "the administrator is written under the tenant the seed created");
            Assert.That(tenant.AccountId, Is.EqualTo(account.Id));
        }
    }

    [Test]
    public async Task NothingIsSeededOrOverwrittenOnceAnyUserExists()
    {
        var account = new Account { Name = "existing" };
        var tenant = new Tenant { AccountId = account.Id, Name = "existing" };
        var existing = new User { TenantId = tenant.Id, Email = "admin@evilcase.test", PasswordHash = "existing", Role = UserRole.User };

        this.context.Accounts.Add(account);
        this.context.Tenants.Add(tenant);
        await this.context.SaveChangesAsync();

        using (this.userContext.Enter(tenant.Id, existing.Id))
        {
            this.context.Users.Add(existing);
            await this.context.SaveChangesAsync();
        }

        await this.Seeder(SeedEmail, SeedPassword).SeedUser(CancellationToken.None);

        var user = await this.context.Users.IgnoreQueryFilters().SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.context.Accounts.IgnoreQueryFilters().CountAsync(), Is.EqualTo(1), "the seed creates nothing once a user exists");
            Assert.That(user.PasswordHash, Is.EqualTo("existing"), "the seed never overwrites a user");
            Assert.That(user.Role, Is.EqualTo(UserRole.User), "the seed never overwrites a user");
        }
    }

    [TestCase(SeedEmail, null)]
    [TestCase(null, SeedPassword)]
    [TestCase(null, null)]
    public async Task AHalfOfTheSeedCreatesNothing(string? email, string? password)
    {
        await this.Seeder(email, password).SeedUser(CancellationToken.None);

        Assert.That(await this.context.Users.IgnoreQueryFilters().AnyAsync(), Is.False);
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
        return this.Seeder(email, password).IsConfigured;
    }

    private UserSeeder Seeder(string? email, string? password)
    {
        var settings = AuthTestHarness.CreateSettings() with { Seed = new() { Email = email, Password = password } };
        var session = new FixedDbSession(this.context);

        return new UserSeeder(session, new UserStore(session), this.userContext, Options.Create(settings), NullLogger<UserSeeder>.Instance);
    }

    private ApplicationDbContext CreateContext(StubUserContext stubUserContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(this.connectionString, static npgsql => npgsql.UseEvilCaseMigrations())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .AddInterceptors(new UserWriteInterceptor(stubUserContext), new HistoryUserInterceptor(stubUserContext))
            .Options;

        return new ApplicationDbContext(options, stubUserContext);
    }
}

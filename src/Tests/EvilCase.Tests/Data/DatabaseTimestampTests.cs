using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// Covers the trigger from SDD-018 against a real PostgreSQL: no fake stands in for a database clock.
/// <see cref="Account"/> carries the columns with no tenant filter, no foreign key and no other
/// required property, so no other write concern gets in the way.
/// </summary>
public class DatabaseTimestampTests
{
    private const string DefaultConnectionString = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    // Named after this checkout: parallel checkouts share one server, and the setup below drops the
    // database it is about to build (.claude/rules/agents.md).
    private static readonly string DatabaseName =
        $"evilcase_tests_timestamps_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory)))[..8].ToLowerInvariant()}";

    private static readonly string ConnectionString =
        $"{Environment.GetEnvironmentVariable("EVILCASE_TEST_POSTGRES") ?? DefaultConnectionString};Database={DatabaseName}";

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    [Test]
    public async Task AnInsertTakesItsCreatedFromTheDatabase()
    {
        await using var context = CreateContext();

        var account = new Account { Name = "insert" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(account.Created, Is.Not.Default, "an insert takes its Created from the database and leaves Updated empty");
            Assert.That(account.Created.Kind, Is.EqualTo(DateTimeKind.Utc), "an insert takes its Created from the database and leaves Updated empty");
            Assert.That(account.Created, Is.EqualTo(DateTime.UtcNow).Within(TimeSpan.FromMinutes(1)), "an insert takes its Created from the database and leaves Updated empty");
            Assert.That(account.Updated, Is.Null, "an insert takes its Created from the database and leaves Updated empty");
        }
    }

    [Test]
    public async Task AWriteThatSetsTheStampsItselfIsIgnored()
    {
        await using var context = CreateContext();

        var account = new Account { Name = "ignored" };
        context.Accounts.Add(account);

        var stamp = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        context.Entry(account).Property(nameof(IEntity.Created)).CurrentValue = stamp;
        context.Entry(account).Property(nameof(IEntity.Updated)).CurrentValue = stamp;

        await context.SaveChangesAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(account.Created, Is.Not.EqualTo(stamp), "the write never sends the stamps, so a value it carries cannot reach the row");
            Assert.That(account.Updated, Is.Null, "the write never sends the stamps, so a value it carries cannot reach the row");
        }
    }

    [Test]
    public async Task AnUpdateStampsUpdatedAndLeavesCreatedAlone()
    {
        await using var context = CreateContext();

        var account = new Account { Name = "before rename" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var created = account.Created;

        context.Entry(account).Property(nameof(Account.Name)).CurrentValue = "renamed";
        await context.SaveChangesAsync();

        await using var reader = CreateContext();
        var persisted = await reader.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(account.Created, Is.EqualTo(created), "an update stamps Updated and leaves Created as the insert wrote it");
            Assert.That(account.Updated, Is.Not.Null.And.GreaterThan(created), "an update stamps Updated and leaves Created as the insert wrote it");
            Assert.That(persisted.Created, Is.EqualTo(account.Created), "an update stamps Updated and leaves Created as the insert wrote it");
            Assert.That(persisted.Updated, Is.EqualTo(account.Updated), "an update stamps Updated and leaves Created as the insert wrote it");
        }
    }

    [Test]
    public async Task AnExecuteUpdateIsStampedTheSameWay()
    {
        await using var context = CreateContext();

        var account = new Account { Name = "before execute update" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var created = account.Created;

        await context.Accounts
            .Where(a => a.Id == account.Id)
            .ExecuteUpdateAsync(setters => setters.SetProperty(a => a.Name, "renamed"));

        await using var reader = CreateContext();
        var persisted = await reader.Accounts.AsNoTracking().SingleAsync(a => a.Id == account.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(persisted.Updated, Is.Not.Null, "a write that goes round the change tracker is stamped by the trigger too");
            Assert.That(persisted.Created, Is.EqualTo(created), "a write that goes round the change tracker is stamped by the trigger too");
        }
    }

    [Test]
    public async Task EveryTableWithTheStampsCarriesTheTrigger()
    {
        await using var context = CreateContext();

        var stamped = context.Model.GetEntityTypes()
            .Where(entityType => typeof(IEntity).IsAssignableFrom(entityType.ClrType))
            .Select(entityType => entityType.GetTableName())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var triggered = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname AS "Value"
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                WHERE t.tgname = 'stamp_timestamps' AND NOT t.tgisinternal
                """)
            .ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(stamped, Is.Not.Empty, "the model maps entities carrying the stamps at all, or this test passes vacuously");
            Assert.That(triggered, Is.SupersetOf(stamped), "a mapped table has no stamp_timestamps trigger, so its rows would be written with no Created");
        }
    }

    private static ApplicationDbContext CreateContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(ConnectionString, npgsql => npgsql.UseEvilCaseMigrations());

        return new ApplicationDbContext(optionsBuilder.Options, new StubUserContext());
    }
}

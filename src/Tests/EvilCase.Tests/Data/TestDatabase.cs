using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// The PostgreSQL the stamp tests write against, in a container the tests start themselves. A test that
/// skips itself hides a broken trigger, so a machine without Docker gets a failure that names what is
/// missing.
/// </summary>
internal static class TestDatabase
{
    private const string Missing =
        "these tests write real rows and read back what the database stamped, so they need Docker — "
            + "Testcontainers starts the PostgreSQL they run against";

    // One slug per run names both the container and the database, so runs side by side never meet.
    private static readonly string Slug = Guid.NewGuid().ToString("N")[..8];

    private static readonly Lazy<string> ConnectionString = new(Connect);

    // Set once the run has started the container, so a run that reached no database takes nothing down.
    private static PostgreSqlContainer? container;

    private static bool migrated;

    /// <summary>
    /// A context over a database built from the migrations.
    /// </summary>
    public static ApplicationDbContext CreateMigrated()
    {
        var context = new ApplicationDbContextFactory().CreateDbContext([]);
        context.Database.SetConnectionString(ConnectionString.Value);

        Migrate(context);

        return context;
    }

    /// <summary>
    /// The same database under a caller's tenant and user, for a test that reads its rows back through the
    /// query filters.
    /// </summary>
    public static ApplicationDbContext CreateMigrated(IUserContext userContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString.Value, npgsql => npgsql.UseEvilCaseMigrations())
            .Options;

        var context = new ApplicationDbContext(options, userContext);

        Migrate(context);

        return context;
    }

    /// <summary>
    /// The same database wired the way the host wires it: no tracking by default and the interceptor that
    /// stamps the tenant and the user on a write.
    /// </summary>
    public static ApplicationDbContext CreateMigratedForWrites(IUserContext userContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString.Value, npgsql => npgsql.UseEvilCaseMigrations())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .AddInterceptors(new UserWriteInterceptor(userContext))
            .Options;

        var context = new ApplicationDbContext(options, userContext);

        Migrate(context);

        return context;
    }

    /// <summary>
    /// Removes the container this run started.
    /// </summary>
    public static async Task Remove()
    {
        if (container is not null)
            await container.DisposeAsync();
    }

    private static string Connect()
    {
        container = new PostgreSqlBuilder("postgres:18-alpine")
            .WithName($"evilcase-test-db-{Slug}")
            .WithDatabase($"evilcase_tests_{Slug}")
            .Build();

        try
        {
            container.StartAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Assert.Fail($"{Missing}; the container did not start: {exception.Message}");
        }

        return container.GetConnectionString();
    }

    private static void Migrate(ApplicationDbContext context)
    {
        if (migrated)
            return;

        context.Database.Migrate();
        migrated = true;
    }
}

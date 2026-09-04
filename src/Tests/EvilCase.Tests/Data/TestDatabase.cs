using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// A test that skips itself hides a broken trigger, so a machine without Docker fails and names what
/// is missing.
/// </summary>
internal static class TestDatabase
{
    private const string Missing =
        "these tests write real rows and read back what the database stamped, so they need Docker — "
            + "Testcontainers starts the PostgreSQL they run against";

    // One slug per run names both the container and the database, so runs side by side never meet.
    private static readonly string Slug = Guid.NewGuid().ToString("N")[..8];

    private static readonly Lazy<string> ConnectionString = new(Connect);

    private static PostgreSqlContainer? container;

    private static bool migrated;

    public static ApplicationDbContext CreateMigrated()
    {
        var context = new ApplicationDbContextFactory().CreateDbContext([]);
        context.Database.SetConnectionString(ConnectionString.Value);

        Migrate(context);

        return context;
    }

    public static ApplicationDbContext CreateMigrated(IUserContext userContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString.Value, static npgsql => npgsql.UseEvilCaseMigrations())
            .Options;

        var context = new ApplicationDbContext(options, userContext);

        Migrate(context);

        return context;
    }

    public static ApplicationDbContext CreateMigratedAsHost(IUserContext userContext)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString.Value, static npgsql => npgsql.UseEvilCaseMigrations())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .AddInterceptors(new UserWriteInterceptor(userContext))
            .Options;

        var context = new ApplicationDbContext(options, userContext);

        Migrate(context);

        return context;
    }

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

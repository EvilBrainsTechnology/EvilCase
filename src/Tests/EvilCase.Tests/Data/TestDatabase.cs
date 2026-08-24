using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// The PostgreSQL the stamp tests write against: a container the tests start themselves, or the server
/// EVILCASE_TEST_POSTGRES names. A test that skips itself hides a broken trigger, so a machine that can
/// supply neither gets a failure that names what is missing.
/// </summary>
internal static class TestDatabase
{
    private const string Missing =
        "these tests write real rows and read back what the database stamped, so they need a server — "
            + "Testcontainers starts one wherever Docker runs, or point EVILCASE_TEST_POSTGRES at a server "
            + "of your own";

    private static readonly Lazy<string> ConnectionString = new(Connect);

    // Named after this checkout: parallel checkouts share one server, and the first caller drops the
    // database it is about to build (.claude/rules/agents.md).
    private static readonly string DatabaseName =
        $"evilcase_tests_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory)))[..8].ToLowerInvariant()}";

    // Set only where this run started the container, so the teardown leaves a server of one's own alone.
    private static PostgreSqlContainer? container;

    private static bool prepared;

    /// <summary>
    /// A context over a database built from the migrations. The first caller in the process rebuilds it,
    /// so no earlier run leaks into this one.
    /// </summary>
    public static ApplicationDbContext CreateMigrated()
    {
        var context = new ApplicationDbContextFactory().CreateDbContext([]);
        context.Database.SetConnectionString(ConnectionString.Value);

        Prepare(context);

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

        Prepare(context);

        return context;
    }

    /// <summary>
    /// Removes the container this run started. A server EVILCASE_TEST_POSTGRES named is left alone.
    /// </summary>
    public static async Task Remove()
    {
        if (container is not null)
            await container.DisposeAsync();
    }

    private static string Connect()
    {
        var server = Environment.GetEnvironmentVariable("EVILCASE_TEST_POSTGRES");

        if (string.IsNullOrEmpty(server))
        {
            // A slug per run: two runs at once must not collide on the name.
            var slug = Guid.NewGuid().ToString("N")[..8];

            container = new PostgreSqlBuilder("postgres:18-alpine")
                .WithName($"evilcase-test-db-{slug}")
                .WithUsername("postgres")
                .WithPassword("postgres")
                .Build();

            try
            {
                container.StartAsync().GetAwaiter().GetResult();
            }
            catch (Exception exception)
            {
                Assert.Fail($"{Missing}; the container did not start: {exception.Message}");
            }

            server = $"Host={container.Hostname};Port={container.GetMappedPublicPort(5432)};Username=postgres;Password=postgres";
        }

        return $"{server};Database={DatabaseName}";
    }

    private static void Prepare(ApplicationDbContext context)
    {
        if (prepared)
            return;

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        prepared = true;
    }
}

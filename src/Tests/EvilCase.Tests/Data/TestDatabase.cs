using System.Security.Cryptography;
using System.Text;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// The PostgreSQL the stamp tests write against. A test that skips itself hides a broken trigger, so a
/// clone without a server gets a failure that names what to start.
/// </summary>
internal static class TestDatabase
{
    private const string DefaultServer = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private const string Missing =
        "these tests write real rows and read back what the database stamped, so they need PostgreSQL — start "
            + "the throwaway one with `docker compose -f deploy/docker-compose.dev.yml up -d --wait` (README.md, "
            + "Local development), or point EVILCASE_TEST_POSTGRES at another server";

    private static readonly string Server = Environment.GetEnvironmentVariable("EVILCASE_TEST_POSTGRES") ?? DefaultServer;

    // Named after this checkout: parallel checkouts share one server, and the first caller drops the
    // database it is about to build (.claude/rules/agents.md).
    private static readonly string DatabaseName =
        $"evilcase_tests_{Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory)))[..8].ToLowerInvariant()}";

    private static readonly string ConnectionString = $"{Server};Database={DatabaseName}";

    private static bool prepared;

    /// <summary>
    /// A context over a database built from the migrations. The first caller in the process rebuilds it,
    /// so no earlier run leaks into this one.
    /// </summary>
    public static ApplicationDbContext CreateMigrated()
    {
        var context = new ApplicationDbContextFactory().CreateDbContext([]);
        context.Database.SetConnectionString(ConnectionString);

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
            .UseNpgsql(ConnectionString, npgsql => npgsql.UseEvilCaseMigrations())
            .Options;

        var context = new ApplicationDbContext(options, userContext);

        Prepare(context);

        return context;
    }

    private static void Prepare(ApplicationDbContext context)
    {
        if (prepared)
            return;

        using (var probe = new NpgsqlConnection($"{Server};Database=postgres"))
        {
            try
            {
                probe.Open();
            }
            catch (NpgsqlException exception) when (exception is not PostgresException)
            {
                Assert.Fail($"{Missing}; the server did not answer: {exception.Message}");
            }
        }

        context.Database.EnsureDeleted();
        context.Database.Migrate();
        prepared = true;
    }
}

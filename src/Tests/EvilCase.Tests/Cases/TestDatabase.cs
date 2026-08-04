using System.Net.Sockets;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// A throwaway PostgreSQL database, migrated from the solution's own migrations. Named apart from every
/// deployment's, and dropped and rebuilt on every run: the connection string names the server, never the
/// database to work in.
/// </summary>
internal static class TestDatabase
{
    /// <summary>
    /// Overrides the server, never the database — the name is built here.
    /// </summary>
    private const string ServerVariable = "EVILCASE_TESTS_POSTGRES";

    private const string DefaultServer = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    /// <summary>
    /// Null where no server answers — CI runs without one, and the caller ignores its tests. Each fixture
    /// names its own database, so two of them never share one.
    /// </summary>
    public static ApplicationDbContext? Create(string name)
    {
        var context = Context(name);

        try
        {
            context.Database.EnsureDeleted();
            context.Database.Migrate();

            return context;
        }
        catch (Exception exception) when (exception is NpgsqlException or SocketException or TimeoutException or InvalidOperationException)
        {
            context.Dispose();

            return null;
        }
    }

    private static ApplicationDbContext Context(string name)
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable) ?? DefaultServer;

        var builder = new NpgsqlConnectionStringBuilder(server) { Database = "evilcase-tests-" + name };

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseEvilCaseMigrations())
            .Options;

        return new ApplicationDbContext(options);
    }
}

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

    /// <summary>
    /// Null only where <see cref="ServerVariable"/> names no server, which is the caller's cue to ignore
    /// its tests. Where it names one, every failure reaching a server is thrown: a refused connection, a
    /// wrong port or a migration that will not apply is a red run, never a silently emptied one. Each
    /// fixture names its own database, so two of them never share one.
    /// </summary>
    public static ApplicationDbContext? Create(string name)
    {
        var server = Environment.GetEnvironmentVariable(ServerVariable);

        if (string.IsNullOrWhiteSpace(server))
            return null;

        var context = Context(server, name);

        context.Database.EnsureDeleted();
        context.Database.Migrate();

        return context;
    }

    private static ApplicationDbContext Context(string server, string name)
    {
        var builder = new NpgsqlConnectionStringBuilder(server) { Database = "evilcase-tests-" + name };

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(builder.ConnectionString, npgsql => npgsql.UseEvilCaseMigrations())
            .Options;

        return new ApplicationDbContext(options);
    }
}

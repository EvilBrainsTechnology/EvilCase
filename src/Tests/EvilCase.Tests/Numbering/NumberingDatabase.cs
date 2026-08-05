using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// A migrated PostgreSQL database of this fixture's own, dropped again on disposal. What one
/// statement does under two callers is the guarantee, so nothing short of a server proves it. Set
/// <c>EVILCASE_TESTS_POSTGRES</c> to a server connection string to test against another server;
/// without a reachable one the tests are ignored, never silently passed.
/// </summary>
internal sealed class NumberingDatabase : IAsyncDisposable
{
    private const string DefaultServer = "Host=localhost;Port=5432;Username=postgres;Password=postgres";

    private readonly string connectionString;

    private NumberingDatabase(string connectionString)
    {
        this.connectionString = connectionString;
    }

    /// <summary>
    /// Creates the database, migrates it and puts <paramref name="owners"/> users in it.
    /// </summary>
    public static async Task<NumberingDatabase> Create(int owners = 1)
    {
        var server = Environment.GetEnvironmentVariable("EVILCASE_TESTS_POSTGRES") ?? DefaultServer;
        var name = "evilcase_tests_" + Guid.NewGuid().ToString("N");
        var database = new NumberingDatabase(new NpgsqlConnectionStringBuilder(server) { Database = name }.ConnectionString);

        await using var context = database.Context();
        try
        {
            await context.Database.MigrateAsync();
        }
        catch (NpgsqlException exception)
        {
            Assert.Ignore($"no PostgreSQL to test the series against, set EVILCASE_TESTS_POSTGRES: {exception.Message}");
        }

        for (var index = 0; index < owners; index++)
        {
            context.Users.Add(new User
            {
                Email = string.Create(CultureInfo.InvariantCulture, $"owner{index}@example.test"),
                PasswordHash = "x",
                Role = UserRole.User,
                Created = DateTime.UtcNow,
            });
        }

        await context.SaveChangesAsync();

        return database;
    }

    /// <summary>
    /// A context of its own, on a connection of its own — two callers of one series must not share one.
    /// </summary>
    public ApplicationDbContext Context()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(this.connectionString, npgsql => npgsql.UseEvilCaseMigrations())
            .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async Task<long> OwnerId(int index = 0)
    {
        await using var context = this.Context();

        var owners = await context.Users.OrderBy(user => user.Id).Select(user => user.Id).ToListAsync();

        return owners[index];
    }

    public async ValueTask DisposeAsync()
    {
        await using var context = this.Context();
        await context.Database.EnsureDeletedAsync();
    }
}

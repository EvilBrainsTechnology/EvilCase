using System.Net.Sockets;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// A migrated PostgreSQL database of one test's own, dropped again on disposal. What one statement
/// does under two callers is the guarantee, so nothing short of a server proves it. Set
/// <c>EVILCASE_TESTS_POSTGRES</c> to a server connection string to test against another server; a
/// server named there and not answering fails, so CI's green says these ran. Without the variable an
/// unreachable server ignores the test instead, never silently passes it.
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
    /// Creates the database, migrates it and puts <paramref name="owners"/> users in it. A failure
    /// anywhere in that drops what it already made, so a broken setup leaves no database behind.
    /// </summary>
    public static async Task<NumberingDatabase> Create(int owners = 1)
    {
        var configured = Environment.GetEnvironmentVariable("EVILCASE_TESTS_POSTGRES");
        var name = "evilcase_tests_" + Guid.NewGuid().ToString("N");
        var database = new NumberingDatabase(new NpgsqlConnectionStringBuilder(configured ?? DefaultServer) { Database = name }.ConnectionString);

        var created = false;
        try
        {
            await database.Fill(owners);
            created = true;
        }
        catch (NpgsqlException exception) when (configured is null)
        {
            Assert.Ignore($"no PostgreSQL to test the series against, set EVILCASE_TESTS_POSTGRES: {exception.Message}");
        }
        finally
        {
            if (!created)
                await database.DisposeAsync();
        }

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

        try
        {
            await context.Database.EnsureDeletedAsync();
        }
        catch (Exception exception) when (exception.GetBaseException() is SocketException)
        {
            // A server that never answered holds no database of ours; a server that refuses the drop
            // says so as a PostgresException and is left to fail.
        }
    }

    private async Task Fill(int owners)
    {
        await using var context = this.Context();

        await context.Database.MigrateAsync();

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
    }
}

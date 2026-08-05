using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
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
    /// Naming <paramref name="stopBefore"/> stops the migrations in front of it, so a test can write
    /// the rows that one has to cope with; <see cref="Migrate"/> then runs it.
    /// </summary>
    public static async Task<NumberingDatabase> Create(int owners = 1, string? stopBefore = null)
    {
        var configured = Environment.GetEnvironmentVariable("EVILCASE_TESTS_POSTGRES");
        var name = "evilcase_tests_" + Guid.NewGuid().ToString("N");
        var database = new NumberingDatabase(new NpgsqlConnectionStringBuilder(configured ?? DefaultServer) { Database = name }.ConnectionString);

        var created = false;
        try
        {
            await database.Fill(owners, stopBefore);
            created = true;
        }
        catch (NpgsqlException exception) when (configured is null)
        {
            Assert.Ignore($"no PostgreSQL to test the series against, set EVILCASE_TESTS_POSTGRES: {exception.Message}");
        }
        finally
        {
            if (!created)
                await database.Drop();
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

    /// <summary>
    /// Runs whatever migrations are still outstanding.
    /// </summary>
    public async Task Migrate()
    {
        await using var context = this.Context();

        await context.Database.MigrateAsync();
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

    private static string Before(DbContext context, string migration)
    {
        var ids = context.GetService<IMigrationsAssembly>().Migrations.Keys.ToList();
        var index = ids.FindIndex(id => id.EndsWith("_" + migration, StringComparison.Ordinal));

        return index > 0 ? ids[index - 1] : throw new ArgumentException($"no migration named {migration} has one in front of it", nameof(migration));
    }

    /// <summary>
    /// Drops whatever the setup managed to make, best effort: the failure that got us here — the
    /// <c>Assert.Ignore</c> above all — is the one worth reporting, and a server that never answered or
    /// refused the credentials holds no database of ours anyway. A drop the server does refuse leaves an
    /// <c>evilcase_tests_*</c> database behind, and says so.
    /// </summary>
    private async Task Drop()
    {
        try
        {
            await this.DisposeAsync();
        }
        catch (NpgsqlException exception)
        {
            var name = new NpgsqlConnectionStringBuilder(this.connectionString).Database;

            await TestContext.Out.WriteLineAsync($"leaving the database {name} behind: {exception.Message}");
        }
    }

    private async Task Fill(int owners, string? stopBefore)
    {
        await using var context = this.Context();

        if (stopBefore is null)
            await context.Database.MigrateAsync();
        else
            await context.GetService<IMigrator>().MigrateAsync(Before(context, stopBefore));

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

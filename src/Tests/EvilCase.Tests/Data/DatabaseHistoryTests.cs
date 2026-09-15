using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// <see cref="Account"/> is the one stamped entity with no tenant filter, foreign key or other
/// required property, so nothing else gets in the way.
/// </summary>
public class DatabaseHistoryTests
{
    [Test]
    public async Task InsertUpdateAndDeleteWriteThreeHistoryRowsInOrder()
    {
        await using var context = TestDatabase.CreateMigrated();

        var account = new Account { Name = "first" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        context.Entry(account).Property(nameof(Account.Name)).CurrentValue = "second";
        await context.SaveChangesAsync();

        context.Accounts.Remove(account);
        await context.SaveChangesAsync();

        var operations = await ReadOperations(context, account.Id);
        var names = await ReadNames(context, account.Id);
        var updated = await ReadUpdated(context, account.Id);

        string[] expectedOperations = ["INSERT", "UPDATE", "DELETE"];
        string[] expectedNames = ["first", "second", "second"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(operations, Is.EqualTo(expectedOperations), "an insert, an update and a delete each write one history row, in the order they happened");
            Assert.That(names, Is.EqualTo(expectedNames), "a history row carries the values that held at the time of its operation");
            Assert.That(updated[0], Is.Null, "the insert row leaves Updated empty, the way stamp_timestamps left it");
            Assert.That(updated[1], Is.Not.Null, "the update row carries a stamped Updated, which proves the record trigger runs after stamp_timestamps");
        }
    }

    [Test]
    public async Task AnExecuteUpdateAndAnExecuteDeleteAreRecordedToo()
    {
        await using var context = TestDatabase.CreateMigrated();

        var account = new Account { Name = "before execute update" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        await context.Accounts
            .Where(a => a.Id == account.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static a => a.Name, "renamed"));

        await context.Accounts
            .Where(a => a.Id == account.Id)
            .ExecuteDeleteAsync();

        var operations = await ReadOperations(context, account.Id);

        string[] expectedOperations = ["INSERT", "UPDATE", "DELETE"];

        Assert.That(operations, Is.EqualTo(expectedOperations), "ExecuteUpdate and ExecuteDelete go round the change tracker but not round the trigger");
    }

    [Test]
    public async Task EveryMappedTableExceptRefreshTokensCarriesTheRecordTrigger()
    {
        await using var context = TestDatabase.CreateMigrated();

        var historized = context.Model.GetEntityTypes()
            .Select(static entityType => entityType.GetTableName())
            .Distinct(StringComparer.Ordinal)
            .Where(static table => !string.Equals(table, "RefreshTokens", StringComparison.Ordinal))
            .ToList();

        var triggered = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname AS "Value"
                FROM pg_trigger t
                JOIN pg_class c ON c.oid = t.tgrelid
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE t.tgname = 'record_history' AND NOT t.tgisinternal AND n.nspname = 'public'
                """)
            .ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(historized, Is.Not.Empty, "the model maps entities carrying history at all, or this test passes vacuously");
            Assert.That(triggered, Is.SupersetOf(historized), "a mapped table other than RefreshTokens has no record_history trigger, so its rows would be written with no history");
            Assert.That(triggered, Does.Not.Contain("RefreshTokens"), "RefreshTokens rotates on every refresh and carries the token hash, so it stays out of history");
        }
    }

    [Test]
    public async Task RefreshTokensHasNoMirrorTable()
    {
        await using var context = TestDatabase.CreateMigrated();

        var mirrors = await context.Database
            .SqlQueryRaw<string>(
                """
                SELECT c.relname AS "Value"
                FROM pg_class c
                JOIN pg_namespace n ON n.oid = c.relnamespace
                WHERE n.nspname = 'history' AND c.relkind = 'r'
                """)
            .ToListAsync();

        Assert.That(mirrors, Does.Not.Contain("RefreshTokens"), "RefreshTokens rotates on every refresh and carries the token hash, so it carries no mirror table");
    }

    [Test]
    public async Task EveryHistoryTableCarriesAtLeastEveryColumnOfItsTable()
    {
        await using var context = TestDatabase.CreateMigrated();

        var historized = context.Model.GetEntityTypes()
            .Select(static entityType => entityType.GetTableName()!)
            .Distinct(StringComparer.Ordinal)
            .Where(static table => !string.Equals(table, "RefreshTokens", StringComparison.Ordinal))
            .ToList();

        using (Assert.EnterMultipleScope())
        {
            foreach (var table in historized)
            {
                var columns = await ReadColumns(context, "public", table);
                var mirrored = await ReadColumns(context, "history", table);

                Assert.That(mirrored, Is.SupersetOf(columns), $"history.\"{table}\" is missing a column \"{table}\" carries, or its type or length drifted");
            }
        }
    }

    private static async Task<List<string>> ReadOperations(ApplicationDbContext context, Guid accountId)
    {
        return await context.Database
            .SqlQuery<string>($"""SELECT "Operation" AS "Value" FROM history."Accounts" WHERE "Id" = {accountId} ORDER BY "OperationAt" """)
            .ToListAsync();
    }

    private static async Task<List<string>> ReadNames(ApplicationDbContext context, Guid accountId)
    {
        return await context.Database
            .SqlQuery<string>($"""SELECT "Name" AS "Value" FROM history."Accounts" WHERE "Id" = {accountId} ORDER BY "OperationAt" """)
            .ToListAsync();
    }

    private static async Task<List<DateTime?>> ReadUpdated(ApplicationDbContext context, Guid accountId)
    {
        return await context.Database
            .SqlQuery<DateTime?>($"""SELECT "Updated" AS "Value" FROM history."Accounts" WHERE "Id" = {accountId} ORDER BY "OperationAt" """)
            .ToListAsync();
    }

    /// <summary>
    /// The schema and table come from our own fixed list, never from input, so they are joined by
    /// hand instead of interpolated — EF1002 flags an interpolated <c>SqlQueryRaw</c> on sight.
    /// </summary>
    private static async Task<List<string>> ReadColumns(ApplicationDbContext context, string schema, string table)
    {
        var sql = "SELECT a.attname || ' ' || format_type(a.atttypid, a.atttypmod) AS \"Value\" "
            + "FROM pg_attribute a JOIN pg_class c ON c.oid = a.attrelid JOIN pg_namespace n ON n.oid = c.relnamespace "
            + "WHERE n.nspname = '"
            + schema
            + "' AND c.relname = '"
            + table
            + "' AND a.attnum > 0 AND NOT a.attisdropped";

        return await context.Database.SqlQueryRaw<string>(sql).ToListAsync();
    }
}

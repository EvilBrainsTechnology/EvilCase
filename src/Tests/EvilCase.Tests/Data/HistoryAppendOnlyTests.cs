using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Data;

public class HistoryAppendOnlyTests
{
    [Test]
    public async Task AnUpdateAgainstAHistoryTableIsRejected()
    {
        await using var context = TestDatabase.CreateMigrated();
        await InsertAccount(context);

        var exception = Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""UPDATE history."Accounts" SET "Operation" = 'INSERT'"""));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.RaiseException), "history.reject_write raises an exception, not a constraint violation");
            Assert.That(exception.MessageText, Does.Contain("Accounts"), "the message names the table an update was rejected on");
        }
    }

    [Test]
    public async Task ADeleteAgainstAHistoryTableIsRejected()
    {
        await using var context = TestDatabase.CreateMigrated();
        await InsertAccount(context);

        var exception = Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""DELETE FROM history."Accounts" """));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.RaiseException), "history.reject_write raises an exception, not a constraint violation");
            Assert.That(exception.MessageText, Does.Contain("Accounts"), "the message names the table a delete was rejected on");
        }
    }

    [Test]
    public async Task ATruncateAgainstAHistoryTableIsRejected()
    {
        await using var context = TestDatabase.CreateMigrated();
        await InsertAccount(context);

        var exception = Assert.ThrowsAsync<PostgresException>(async () =>
            await context.Database.ExecuteSqlRawAsync("""TRUNCATE history."Accounts" """));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception!.SqlState, Is.EqualTo(PostgresErrorCodes.RaiseException), "history.reject_write raises an exception, not a constraint violation");
            Assert.That(exception.MessageText, Does.Contain("Accounts"), "the message names the table a truncate was rejected on");
        }
    }

    /// <summary>
    /// A row trigger that matches no row never fires, so every rejection test needs a row first.
    /// </summary>
    private static async Task InsertAccount(ApplicationDbContext context)
    {
        var account = new Account { Name = "append only" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();
    }
}

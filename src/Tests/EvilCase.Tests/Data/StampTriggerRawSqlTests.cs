using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

public class StampTriggerRawSqlTests
{
    private static readonly DateTime HandSet = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private ApplicationDbContext context = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        this.context = TestDatabase.CreateMigrated();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        this.context.Dispose();
    }

    [Test]
    public void AnInsertThatNamesTheStampsGetsThemFromTheDatabase()
    {
        var accountId = Guid.CreateVersion7();

        this.InsertAccount(accountId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                this.ReadCreated(accountId),
                Is.GreaterThan(DateTime.UtcNow.AddMinutes(-5)),
                "an insert outside EF takes Created from the database, not from the statement");
            Assert.That(
                this.ReadUpdated(accountId),
                Is.Null,
                "an insert leaves Updated empty however the statement names it");
        }
    }

    [Test]
    public void AnUpdateThatNamesTheStampsKeepsCreatedAndGetsUpdated()
    {
        var accountId = Guid.CreateVersion7();

        this.InsertAccount(accountId);

        var created = this.ReadCreated(accountId);

        this.context.Database.ExecuteSql(
            $"""
            UPDATE "Accounts"
            SET "Name" = 'raw update', "Created" = {HandSet}, "Updated" = {HandSet}
            WHERE "Id" = {accountId}
            """);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                this.ReadCreated(accountId),
                Is.EqualTo(created),
                "an update outside EF cannot move Created, whatever the statement sets");
            Assert.That(
                this.ReadUpdated(accountId),
                Is.GreaterThan(DateTime.UtcNow.AddMinutes(-5)),
                "an update outside EF takes Updated from the database, not from the statement");
        }
    }

    private void InsertAccount(in Guid accountId)
    {
        this.context.Database.ExecuteSql(
            $"""
            INSERT INTO "Accounts" ("Id", "Name", "Created", "Updated")
            VALUES ({accountId}, 'raw insert', {HandSet}, {HandSet})
            """);
    }

    private DateTime ReadCreated(in Guid accountId)
    {
        return this.context.Database
            .SqlQuery<DateTime>($"""SELECT "Created" AS "Value" FROM "Accounts" WHERE "Id" = {accountId}""")
            .Single();
    }

    private DateTime? ReadUpdated(in Guid accountId)
    {
        return this.context.Database
            .SqlQuery<DateTime?>($"""SELECT "Updated" AS "Value" FROM "Accounts" WHERE "Id" = {accountId}""")
            .Single();
    }
}

using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// What the stamp trigger does to a write that never passes through EF: raw SQL naming Created and
/// Updated itself.
/// </summary>
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
        var id = Guid.CreateVersion7();

        this.Insert(id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                this.ReadCreated(id),
                Is.GreaterThan(DateTime.UtcNow.AddMinutes(-5)),
                "an insert outside EF takes Created from the database, not from the statement");
            Assert.That(
                this.ReadUpdated(id),
                Is.Null,
                "an insert leaves Updated empty however the statement names it");
        }
    }

    [Test]
    public void AnUpdateThatNamesTheStampsKeepsCreatedAndGetsUpdated()
    {
        var id = Guid.CreateVersion7();

        this.Insert(id);

        var created = this.ReadCreated(id);

        this.context.Database.ExecuteSql(
            $"""
            UPDATE "Accounts"
            SET "Name" = 'raw update', "Created" = {HandSet}, "Updated" = {HandSet}
            WHERE "Id" = {id}
            """);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                this.ReadCreated(id),
                Is.EqualTo(created),
                "an update outside EF cannot move Created, whatever the statement sets");
            Assert.That(
                this.ReadUpdated(id),
                Is.GreaterThan(DateTime.UtcNow.AddMinutes(-5)),
                "an update outside EF takes Updated from the database, not from the statement");
        }
    }

    private void Insert(in Guid id)
    {
        this.context.Database.ExecuteSql(
            $"""
            INSERT INTO "Accounts" ("Id", "Name", "Created", "Updated")
            VALUES ({id}, 'raw insert', {HandSet}, {HandSet})
            """);
    }

    private DateTime ReadCreated(in Guid id)
    {
        return this.context.Database
            .SqlQuery<DateTime>($"""SELECT "Created" AS "Value" FROM "Accounts" WHERE "Id" = {id}""")
            .Single();
    }

    private DateTime? ReadUpdated(in Guid id)
    {
        return this.context.Database
            .SqlQuery<DateTime?>($"""SELECT "Updated" AS "Value" FROM "Accounts" WHERE "Id" = {id}""")
            .Single();
    }
}

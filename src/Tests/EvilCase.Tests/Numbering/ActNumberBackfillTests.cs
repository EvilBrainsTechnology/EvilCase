using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The <c>Numbering</c> migration against acts that were already written. Its unique index refuses two
/// acts of one case on the empty default, so what it backfills has to be distinct within the case —
/// and only a server runs the statement that does it.
/// </summary>
public class ActNumberBackfillTests
{
    private NumberingDatabase? database;

    private NumberingDatabase Database => this.database!;

    [SetUp]
    public async Task SetUp() => this.database = await NumberingDatabase.Create(stopBefore: "Numbering");

    // Without a server SetUp ignores the test and leaves nothing here to drop.
    [TearDown]
    public async Task TearDown()
    {
        if (this.database is not null)
            await this.database.DisposeAsync();
    }

    [Test]
    public async Task TwoActsOfOneCaseComeOutUnderNumbersOfTheirOwn()
    {
        await this.WriteTwoActsOfOneCase();

        await this.Database.Migrate();

        var numbers = await this.ActNumbers();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(numbers.Distinct(), Has.Exactly(2).Items, "two acts of one case under one number are what the index refuses");
            Assert.That(numbers, Is.All.StartsWith("EC-1-"), "an act already written is numbered from its case's own mark");
        }
    }

    private async Task WriteTwoActsOfOneCase()
    {
        var ownerId = await this.Database.OwnerId();

        await using var context = this.Database.Context();

        await context.Database.ExecuteSqlAsync(
            $"""
             INSERT INTO "Cases" ("OwnerId", "CaseNumber", "Title", "Status", "Created")
             VALUES ({ownerId}, 'EC-1', 'a case written before the numbering', 'Active', now());

             INSERT INTO "Acts" ("CaseId", "Direction", "Title", "Date", "Created")
             SELECT "Id", 'Incoming', 'the first act', DATE '2026-01-02', now() FROM "Cases" WHERE "CaseNumber" = 'EC-1';

             INSERT INTO "Acts" ("CaseId", "Direction", "Title", "Date", "Created")
             SELECT "Id", 'Outgoing', 'the second act', DATE '2026-01-03', now() FROM "Cases" WHERE "CaseNumber" = 'EC-1';
             """);
    }

    private async Task<List<string>> ActNumbers()
    {
        await using var context = this.Database.Context();

        return await context.Acts.Select(act => act.ActNumber).ToListAsync();
    }
}

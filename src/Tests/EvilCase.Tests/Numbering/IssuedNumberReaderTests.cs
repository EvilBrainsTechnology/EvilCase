using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Numbering;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// What one series already holds, read out of the columns the numbers were written into. The reader
/// narrows the rows by the series' prefix and the server is what says that filter is text rather than
/// a pattern of its own.
/// </summary>
public class IssuedNumberReaderTests
{
    private static readonly DateOnly Date = new(2026, 8, 4);

    private NumberingDatabase? database;

    private long ownerId;

    private NumberingDatabase Database => this.database!;

    [SetUp]
    public async Task SetUp()
    {
        this.database = await NumberingDatabase.Create(owners: 2);
        this.ownerId = await this.Database.OwnerId();
    }

    // Without a server SetUp ignores the test and leaves nothing here to drop.
    [TearDown]
    public async Task TearDown()
    {
        if (this.database is not null)
            await this.database.DisposeAsync();
    }

    [Test]
    public async Task ASeriesWithNothingInItIsZeroSoTheFirstNumberIsOne()
    {
        Assert.That(await this.HighestCaseNumber(), Is.Zero);
    }

    [Test]
    public async Task TheHighestNumberOfTheSeriesIsWhatCounts()
    {
        await this.WriteCase("EC-20260804-002");
        await this.WriteCase("EC-20260804-041");
        await this.WriteCase("EC-20260804-007");

        Assert.That(await this.HighestCaseNumber(), Is.EqualTo(41), "the next number is the one after the highest, not after the last written");
    }

    [Test]
    public async Task AnotherDayAndAMarkOfAnotherShapeAreNotThisSeries()
    {
        await this.WriteCase("EC-20260805-900");
        await this.WriteCase("OLD-2019/16");
        await this.WriteCase("EC-20260804-003");

        Assert.That(await this.HighestCaseNumber(), Is.EqualTo(3));
    }

    [Test]
    public async Task OneOwnerNeverCountsInAnothers()
    {
        await this.WriteCase("EC-20260804-500", await this.Database.OwnerId(index: 1));
        await this.WriteCase("EC-20260804-004");

        Assert.That(await this.HighestCaseNumber(), Is.EqualTo(4), "a series belongs to its owner, whatever another owner's numbers say");
    }

    /// <summary>
    /// The escape the prefix is built with is the one the <c>LIKE</c> names; a server that reads the
    /// backslash as a character of its own would read no rows at all.
    /// </summary>
    [Test]
    public async Task TheEscapedPrefixReadsTheRowsItNames()
    {
        await this.WriteCase("A_C-2026-008");

        Assert.That(await this.HighestCaseNumber("A_C-{year}-{seq}"), Is.EqualTo(8));
    }

    /// <summary>
    /// A case number is unique within its owner and no further, so two owners can hold the same one and
    /// write act numbers of the same text under it. The case is what tells the two series apart.
    /// </summary>
    [Test]
    public async Task AnActCountsWithinItsOwnCaseRatherThanEveryCaseOfThatNumber()
    {
        var mine = await this.WriteCase("EC-20260804-001");
        var another = await this.WriteCase("EC-20260804-002");
        var theirs = await this.WriteCase("EC-20260804-001", await this.Database.OwnerId(index: 1));

        await this.WriteAct(mine, "EC-20260804-001-20260804-005");
        await this.WriteAct(another, "EC-20260804-002-20260804-800");
        await this.WriteAct(theirs, "EC-20260804-001-20260804-900");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await this.HighestActNumber(mine, "EC-20260804-001"), Is.EqualTo(5), "another owner's case of the same number is another case");
            Assert.That(await this.HighestActNumber(another, "EC-20260804-002"), Is.EqualTo(800), "and so is a case of the caller's own carrying another number");
        }
    }

    private async Task<int> HighestCaseNumber(string pattern = NumberingDefaults.CaseNumberPattern)
    {
        await using var context = this.Database.Context();

        return await new IssuedNumberReader(context, new FixedOwnerContext(this.ownerId))
            .HighestCaseNumber(NumberPattern.Series(pattern, Date));
    }

    private async Task<int> HighestActNumber(long caseId, string caseNumber)
    {
        await using var context = this.Database.Context();

        return await new IssuedNumberReader(context, new FixedOwnerContext(this.ownerId))
            .HighestActNumber(caseId, NumberPattern.Series(NumberingDefaults.ActNumberPattern, Date, caseNumber));
    }

    private async Task<long> WriteCase(string caseNumber, long? ownerId = null)
    {
        await using var context = this.Database.Context();

        var written = new Case
        {
            OwnerId = ownerId ?? this.ownerId,
            CaseNumber = caseNumber,
            Title = "a case carrying a number",
            Status = CaseStatus.Active,
            Created = DateTime.UtcNow,
        };

        context.Cases.Add(written);
        await context.SaveChangesAsync();

        return written.Id;
    }

    private async Task WriteAct(long caseId, string actNumber)
    {
        await using var context = this.Database.Context();

        context.Acts.Add(new Act
        {
            CaseId = caseId,
            Direction = ActDirection.Incoming,
            Title = "an act carrying a number",
            ActNumber = actNumber,
            Date = Date,
            Created = DateTime.UtcNow,
        });

        await context.SaveChangesAsync();
    }
}

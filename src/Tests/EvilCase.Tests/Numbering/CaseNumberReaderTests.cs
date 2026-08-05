using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The number an act is written under, read from the case it hangs on.
/// </summary>
public class CaseNumberReaderTests
{
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
    public async Task ACaseCarriesTheNumberItsActsAreWrittenUnder()
    {
        var caseId = await this.WriteCase("OLD-2019/16");

        Assert.That(await this.Read(caseId), Is.EqualTo("OLD-2019/16"), "the number is the case's own, whether it was issued or typed in");
    }

    [Test]
    public void ACaseThatIsNotThereIsRefusedRatherThanNumberedUnderNothing()
    {
        Assert.That(async () => await this.Read(caseId: 404), Throws.InstanceOf<CaseNotFoundException>(), "a missing case is its own failure, not the broken query an InvalidOperationException would say it is");
    }

    [Test]
    public async Task AnotherOwnersCaseIsAsGoodAsNoCase()
    {
        var theirs = await this.WriteCase("EC-20260804-001", await this.Database.OwnerId(index: 1));

        Assert.That(async () => await this.Read(theirs), Throws.InstanceOf<CaseNotFoundException>(), "a number is written under a case of the caller's own, never under one they cannot see");
    }

    private async Task<string> Read(long caseId)
    {
        await using var context = this.Database.Context();

        return await new CaseNumberReader(context, new FixedOwnerContext(this.ownerId)).Read(caseId);
    }

    private async Task<long> WriteCase(string caseNumber, long? ownerId = null)
    {
        await using var context = this.Database.Context();

        var written = new Case
        {
            OwnerId = ownerId ?? this.ownerId,
            CaseNumber = caseNumber,
            Title = "a case to hang an act on",
            Status = CaseStatus.Active,
            Created = DateTime.UtcNow,
        };

        context.Cases.Add(written);
        await context.SaveChangesAsync();

        return written.Id;
    }
}

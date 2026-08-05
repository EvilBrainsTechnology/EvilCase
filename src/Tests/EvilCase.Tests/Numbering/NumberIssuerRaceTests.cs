using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The next number is the one after the highest already stored, so two callers reading it at once
/// build the same one and the unique index refuses the second. Only a server runs that index, so
/// nothing short of one says the retry holds.
/// </summary>
public class NumberIssuerRaceTests
{
    private const int Callers = 20;

    private NumberingDatabase? database;

    private long ownerId;

    private NumberingDatabase Database => this.database!;

    [SetUp]
    public async Task SetUp()
    {
        this.database = await NumberingDatabase.Create();
        this.ownerId = await this.Database.OwnerId();
    }

    // Without a server SetUp ignores the test and leaves nothing here to drop.
    [TearDown]
    public async Task TearDown()
    {
        if (this.database is not null)
            await this.database.DisposeAsync();
    }

    /// <summary>
    /// One caller holds its row uncommitted while the other reads the same maximum, builds the same
    /// number and waits on the index for it. The commit turns that wait into a refusal, and the
    /// refusal into the number after it.
    /// </summary>
    [Test]
    public async Task TheCallerWhoseNumberIsTakenTakesTheOneAfterIt()
    {
        await using var firstContext = this.Database.Context();
        await using var secondContext = this.Database.Context();

        await using var transaction = await firstContext.Database.BeginTransactionAsync();
        var first = await this.Issuer(firstContext).IssueCaseNumber((number, token) => WriteCase(firstContext, this.ownerId, number, token));

        var second = this.Issuer(secondContext).IssueCaseNumber((number, token) => WriteCase(secondContext, this.ownerId, number, token));
        var raced = await Task.WhenAny(second, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.That(raced, Is.Not.SameAs(second), "the second caller waits on the index for the row the first one is holding");

        await transaction.CommitAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo("EC-20260804-001"));
            Assert.That(await second, Is.EqualTo("EC-20260804-002"), "the refused caller reads the maximum again rather than handing out the number it already built");
            Assert.That(await this.CaseNumbers(), Has.Exactly(2).Items, "and the attempt the index refused leaves no second row of its own");
        }
    }

    [Test]
    public async Task EveryOneOfManyCallersAtOnceEndsUnderANumberOfItsOwn()
    {
        var contexts = Enumerable.Range(0, Callers).Select(_ => this.Database.Context()).ToList();

        try
        {
            var numbers = await Task.WhenAll(contexts.Select(context =>
                this.Issuer(context).IssueCaseNumber((number, token) => WriteCase(context, this.ownerId, number, token))));

            var expected = Enumerable.Range(1, Callers).Select(sequence => string.Create(CultureInfo.InvariantCulture, $"EC-20260804-{sequence:D3}"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(numbers.Distinct(), Has.Exactly(Callers).Items, "a number handed out twice is two cases under one file mark");
                Assert.That(numbers.Order(StringComparer.Ordinal), Is.EqualTo(expected), "the series counts on without a gap");
                Assert.That(await this.CaseNumbers(), Has.Exactly(Callers).Items);
            }
        }
        finally
        {
            foreach (var context in contexts)
                await context.DisposeAsync();
        }
    }

    private static async Task<string> WriteCase(ApplicationDbContext context, long ownerId, string number, CancellationToken cancellationToken)
    {
        context.Cases.Add(new Case
        {
            OwnerId = ownerId,
            CaseNumber = number,
            Title = "a case taking the next number",
            Status = CaseStatus.Active,
            Created = DateTime.UtcNow,
        });

        await context.SaveChangesAsync(cancellationToken);

        return number;
    }

    private async Task<List<string>> CaseNumbers()
    {
        await using var context = this.Database.Context();

        return await context.Cases.Select(@case => @case.CaseNumber).ToListAsync();
    }

    private NumberIssuer Issuer(ApplicationDbContext context)
    {
        var owner = new FixedOwnerContext(this.ownerId);

        return new NumberIssuer(
            new FakeNumberingSettingsReader(),
            new IssuedNumberReader(context, owner),
            new CaseNumberReader(context, owner),
            new TestTimeProvider(new DateTime(2026, 8, 4, 9, 0, 0, DateTimeKind.Utc)));
    }
}

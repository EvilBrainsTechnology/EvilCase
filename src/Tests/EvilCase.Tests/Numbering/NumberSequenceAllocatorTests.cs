using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// The allocator against PostgreSQL. The guarantee is what two callers of one series get, so the
/// server is the only thing that can say whether it holds.
/// </summary>
public class NumberSequenceAllocatorTests
{
    private const int Callers = 20;

    private NumberingDatabase database = null!;

    private long ownerId;

    [SetUp]
    public async Task SetUp()
    {
        this.database = await NumberingDatabase.Create(owners: 2);
        this.ownerId = await this.database.OwnerId();
    }

    [TearDown]
    public async Task TearDown() => await this.database.DisposeAsync();

    [Test]
    public async Task ASeriesCountsFromOneAndKeepsGoing()
    {
        await using var context = this.database.Context();
        var allocator = this.Allocator(context);

        int[] taken = [await allocator.Next("case:20260805"), await allocator.Next("case:20260805"), await allocator.Next("case:20260805")];
        int[] expected = [1, 2, 3];

        Assert.That(taken, Is.EqualTo(expected));
    }

    [Test]
    public async Task EveryScopeCountsOnItsOwn()
    {
        await using var context = this.database.Context();
        var allocator = this.Allocator(context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(await allocator.Next("case:20260805"), Is.EqualTo(1));
            Assert.That(await allocator.Next("act:1:20260805"), Is.EqualTo(1), "another series starts at its own beginning");
            Assert.That(await allocator.Next("case:20260805"), Is.EqualTo(2));
        }
    }

    [Test]
    public async Task OneOwnerNeverCountsInAnothersSeries()
    {
        var other = await this.database.OwnerId(index: 1);

        await using var mine = this.database.Context();
        await using var theirs = this.database.Context();

        var first = await this.Allocator(mine).Next("case:20260805");
        var second = await new NumberSequenceAllocator(theirs, new FixedOwnerContext(other)).Next("case:20260805");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(first, Is.EqualTo(1));
            Assert.That(second, Is.EqualTo(1), "a series belongs to its owner, so another owner's first number is one");
            Assert.That(await this.Rows(), Is.EqualTo(2), "two owners on one scope are two series");
        }
    }

    /// <summary>
    /// A caller inside an uncommitted transaction holds the row; the next one waits for it and takes
    /// the value after it, rather than reading the counter it has not yet raised.
    /// </summary>
    [Test]
    public async Task ACallerWaitsForTheOneHoldingTheRowAndTakesTheValueAfterIt()
    {
        await using var holder = this.database.Context();
        await using var waiter = this.database.Context();

        Assert.That(await this.Allocator(holder).Next("case:20260805"), Is.EqualTo(1));

        await using var transaction = await holder.Database.BeginTransactionAsync();
        var held = await this.Allocator(holder).Next("case:20260805");

        var pending = this.Allocator(waiter).Next("case:20260805");
        var raced = await Task.WhenAny(pending, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.That(raced, Is.Not.SameAs(pending), "the second caller waits on the row the first one is holding");

        await transaction.CommitAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(held, Is.EqualTo(2));
            Assert.That(await pending, Is.EqualTo(3), "the waiting caller raises what the row now holds, not what it read before");
        }
    }

    [Test]
    public async Task AValueTakenInsideATransactionThatRollsBackGoesBackWithIt()
    {
        await using var context = this.database.Context();
        var allocator = this.Allocator(context);

        await using (var transaction = await context.Database.BeginTransactionAsync())
        {
            Assert.That(await allocator.Next("case:20260805"), Is.EqualTo(1));
            await transaction.RollbackAsync();
        }

        Assert.That(await allocator.Next("case:20260805"), Is.EqualTo(1), "the series counts up, but it counts up over what was committed");
    }

    [Test]
    public async Task EveryOneOfManyCallersAtOnceTakesAValueOfItsOwn()
    {
        var contexts = Enumerable.Range(0, Callers).Select(_ => this.database.Context()).ToList();

        try
        {
            var taken = await Task.WhenAll(contexts.Select(context => this.Allocator(context).Next("case:20260805")));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(taken.Distinct(), Has.Exactly(Callers).Items, "a number handed out twice is two cases under one file mark");
                Assert.That(taken.Order(), Is.EqualTo(Enumerable.Range(1, Callers)), "the series counts on without a gap");
            }
        }
        finally
        {
            foreach (var context in contexts)
                await context.DisposeAsync();
        }
    }

    private async Task<int> Rows()
    {
        await using var context = this.database.Context();

        return await context.NumberSequences.CountAsync();
    }

    private NumberSequenceAllocator Allocator(ApplicationDbContext context) =>
        new(context, new FixedOwnerContext(this.ownerId));
}

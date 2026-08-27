using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// The act list across every case on the rows a real PostgreSQL returns. Each test seeds a tenant of
/// its own, so none cleans up after itself.
/// </summary>
public class ActListAcrossCasesTests
{
    private TestTenant tenant = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task TheActDateOrdersNewestFirstAcrossEveryCase()
    {
        var first = await this.tenant.AddCase(new DateOnly(2026, 8, 20), "První spis");
        var second = await this.tenant.AddCase(new DateOnly(2026, 8, 20), "Druhý spis");

        var middle = await this.tenant.AddAct(first, new DateOnly(2026, 8, 22), "Výzva");
        var newest = await this.tenant.AddAct(second, new DateOnly(2026, 8, 24), "Rozhodnutí");
        var oldest = await this.tenant.AddAct(first, new DateOnly(2026, 8, 20), "Podání");

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);

        Guid[] expected = [newest.Id, middle.Id, oldest.Id];

        Assert.That(
            items.Select(item => item.ActId),
            Is.EqualTo(expected),
            "the act list crosses cases and puts the newest act date first");
    }

    [Test]
    public async Task AnActOfAnotherTenantNeverComesBack()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var mine = await this.tenant.AddAct(@case, new DateOnly(2026, 8, 22), "Podání");

        await using (var other = await TestTenant.Create())
        {
            var otherCase = await other.AddCase(new DateOnly(2026, 8, 20));
            await other.AddAct(otherCase, new DateOnly(2026, 8, 21), "Cizí úkon");
        }

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);

        Guid[] expected = [mine.Id];

        Assert.That(
            items.Select(item => item.ActId),
            Is.EqualTo(expected),
            "the tenant query filter is what keeps another tenant's acts out of the act list");
    }

    [Test]
    public async Task TheCapReturnsOnlyTheNewestActs()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 15));
        var acts = new List<Act>();

        for (var day = 15; day <= 21; day++)
            acts.Add(await this.tenant.AddAct(@case, new DateOnly(2026, 8, day), $"Úkon {day.ToString(CultureInfo.InvariantCulture)}"));

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));

        var items = await reader.ListActs(new ActListRequest { Take = 5 }, CancellationToken.None);

        var expected = acts.TakeLast(5).Reverse().Select(act => act.Id);

        Assert.That(
            items.Select(item => item.ActId),
            Is.EqualTo(expected),
            "the dashboard tile's five is a cap the database applies, not a slice the caller takes");
    }

    [Test]
    public async Task AnAbsentCapReturnsEveryAct()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 15));

        for (var day = 15; day <= 21; day++)
            await this.tenant.AddAct(@case, new DateOnly(2026, 8, day), $"Úkon {day.ToString(CultureInfo.InvariantCulture)}");

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));

        var items = await reader.ListActs(new ActListRequest { Take = null }, CancellationToken.None);

        Assert.That(items, Has.Count.EqualTo(7), "an absent cap narrows nothing");
    }

    [Test]
    public async Task ARowNamesTheCaseTheActBelongsTo()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        await this.tenant.AddAct(@case, new DateOnly(2026, 8, 21), "Podání");

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);
        var item = items.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.CaseId, Is.EqualTo(@case.Id), "a row names the case, which is what the dashboard links to");
            Assert.That(item.CaseNumber, Is.EqualTo(@case.CaseNumber), "a row names the case, which is what the dashboard links to");
        }
    }
}

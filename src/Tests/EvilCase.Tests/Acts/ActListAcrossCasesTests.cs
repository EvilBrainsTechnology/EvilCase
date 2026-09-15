using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActListAcrossCasesTests : TenantFixture
{
    [Test]
    public async Task TheChangeOrderPutsTheLastChangedActFirstAcrossEveryCase()
    {
        var first = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "První spis");
        var second = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "Druhý spis");

        var oldest = await this.Tenant.AddAct(first, new DateOnly(2026, 8, 20), "Podání");
        var middle = await this.Tenant.AddAct(first, new DateOnly(2026, 8, 22), "Výzva");
        var newest = await this.Tenant.AddAct(second, new DateOnly(2026, 8, 24), "Rozhodnutí");

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);

        Guid[] expected = [newest.Id, middle.Id, oldest.Id];

        Assert.That(
            items.Select(static item => item.ActId),
            Is.EqualTo(expected),
            "the act list crosses cases and orders by each act's own Created while none was edited");
    }

    [Test]
    public async Task EditingAnActMovesItToTheFront()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 20));

        var a = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 20), "A");
        var b = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 21), "B");
        var c = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 22), "C");

        await this.Tenant.Context.Acts.Where(act => act.Id == a.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static act => act.Title, "A upravený"));

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);

        Guid[] expected = [a.Id, c.Id, b.Id];

        Assert.That(
            items.Select(static item => item.ActId),
            Is.EqualTo(expected),
            "an edited act's own Updated moves it to the front, ahead of acts never touched");
    }

    [Test]
    public async Task AnActOfAnotherTenantNeverComesBack()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 20));
        var mine = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 22), "Podání");

        await using (var other = await TestTenant.Create())
        {
            var otherCase = await other.AddCase(new DateOnly(2026, 8, 20));
            await other.AddAct(otherCase, new DateOnly(2026, 8, 21), "Cizí úkon");
        }

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);

        Guid[] expected = [mine.Id];

        Assert.That(
            items.Select(static item => item.ActId),
            Is.EqualTo(expected),
            "the tenant query filter is what keeps another tenant's acts out of the act list");
    }

    [Test]
    public async Task TheCapReturnsOnlyTheNewestActs()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 15));
        var acts = new List<Act>();

        for (var day = 15; day <= 21; day++)
            acts.Add(await this.Tenant.AddAct(@case, new DateOnly(2026, 8, day), $"Úkon {day.ToString(CultureInfo.InvariantCulture)}"));

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var items = await reader.ListActs(new ActListRequest { Take = 5 }, CancellationToken.None);

        var expected = acts.TakeLast(5).Reverse().Select(static act => act.Id);

        Assert.That(
            items.Select(static item => item.ActId),
            Is.EqualTo(expected),
            "the dashboard tile's five is a cap the database applies, not a slice the caller takes");
    }

    [Test]
    public async Task AnAbsentCapReturnsEveryAct()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 15));

        for (var day = 15; day <= 21; day++)
            await this.Tenant.AddAct(@case, new DateOnly(2026, 8, day), $"Úkon {day.ToString(CultureInfo.InvariantCulture)}");

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var items = await reader.ListActs(new ActListRequest { Take = null }, CancellationToken.None);

        Assert.That(items, Has.Count.EqualTo(7), "an absent cap narrows nothing");
    }

    [Test]
    public async Task ARowNamesTheCaseTheActBelongsTo()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "Přestupek");
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 21), "Podání");

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var items = await reader.ListActs(new ActListRequest(), CancellationToken.None);
        var item = items.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.CaseId, Is.EqualTo(@case.Id), "a row names the case, which is what the dashboard links to");
            Assert.That(item.CaseNumber, Is.EqualTo(@case.CaseNumber), "a row names the case, which is what the dashboard links to");
            Assert.That(item.CaseTitle, Is.EqualTo(@case.Title), "a row also names the case by title, for the dashboard tile's Název column");
            Assert.That(item.Changed, Is.EqualTo(act.Created), "a row shows the act's own last change, its Created while it has never been edited");
        }
    }
}

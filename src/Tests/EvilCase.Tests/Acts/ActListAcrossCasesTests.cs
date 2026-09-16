using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
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

        var items = await this.List(new ActListRequest { Take = 20, Sort = ActSortKey.Changed });

        Guid[] expected = [newest.Id, middle.Id, oldest.Id];

        Assert.That(items, Is.EqualTo(expected), "the act list crosses cases and orders by each act's own Created while none was edited");
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

        var items = await this.List(new ActListRequest { Take = 20, Sort = ActSortKey.Changed });

        Guid[] expected = [a.Id, c.Id, b.Id];

        Assert.That(items, Is.EqualTo(expected), "an edited act's own Updated moves it to the front, ahead of acts never touched");
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

        var items = await this.List(new ActListRequest { Take = 20 });

        Guid[] expected = [mine.Id];

        Assert.That(items, Is.EqualTo(expected), "the tenant query filter is what keeps another tenant's acts out of the act list");
    }

    [Test]
    public async Task TheCaseNarrowsTheListTheOtherFiltersLeaveAlone()
    {
        var first = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "První spis");
        var second = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "Druhý spis");

        var wanted = await this.Tenant.AddAct(first, new DateOnly(2026, 8, 21), "Podání");
        await this.Tenant.AddAct(second, new DateOnly(2026, 8, 21), "Jiný úkon");

        var response = await this.Reader().ListActs(new ActListRequest { Take = 20, CaseId = first.Id }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Items.Select(static item => item.ActId), Is.EqualTo([wanted.Id]), "the act list narrows to one case where the request names it");
            Assert.That(response.TotalCount, Is.EqualTo(1), "the total counts what the case filter leaves");
        }
    }

    [Test]
    public async Task ARowNamesTheCaseTheActBelongsTo()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "Přestupek");
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 21), "Podání");

        var response = await this.Reader().ListActs(new ActListRequest { Take = 20 }, CancellationToken.None);
        var item = response.Items.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.CaseId, Is.EqualTo(@case.Id), "a row names the case, which is what the dashboard links to");
            Assert.That(item.CaseNumber, Is.EqualTo(@case.CaseNumber), "a row names the case, which is what the dashboard links to");
            Assert.That(item.CaseTitle, Is.EqualTo(@case.Title), "a row also names the case by title, for the dashboard tile's Název column");
            Assert.That(item.Changed, Is.EqualTo(act.Created), "a row shows the act's own last change, its Created while it has never been edited");
        }
    }

    private async Task<List<Guid>> List(ActListRequest request)
    {
        var response = await this.Reader().ListActs(request, CancellationToken.None);

        return [.. response.Items.Select(static item => item.ActId)];
    }

    private ActReader Reader()
    {
        return new ActReader(new FixedDbSession(this.Tenant.Context));
    }
}

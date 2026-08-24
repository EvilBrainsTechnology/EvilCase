using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// The act order on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself.
/// </summary>
public class ActListQueryTests
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
    public async Task TheActDateOrdersOldestFirst()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var latest = await this.tenant.AddAct(@case, new DateOnly(2026, 8, 24), "Rozhodnutí");
        var earliest = await this.tenant.AddAct(@case, new DateOnly(2026, 8, 20), "Podání");
        var middle = await this.tenant.AddAct(@case, new DateOnly(2026, 8, 22), "Výzva");

        var ids = await this.tenant.Context.Acts.InListOrder().Select(act => act.Id).ToListAsync();

        Guid[] expected = [earliest.Id, middle.Id, latest.Id];

        Assert.That(ids, Is.EqualTo(expected), "act lists are ordered by the act date, oldest first");
    }

    [Test]
    public async Task TheIdentifierBreaksATieOnTheDate()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var sameDay = new DateOnly(2026, 8, 22);
        var first = await this.tenant.AddAct(@case, sameDay, "Podání");
        var second = await this.tenant.AddAct(@case, sameDay, "Výzva");
        var third = await this.tenant.AddAct(@case, sameDay, "Rozhodnutí");

        var ids = await this.tenant.Context.Acts.InListOrder().Select(act => act.Id).ToListAsync();

        // The identifier is a UUIDv7, so the ascending order is the order the acts were written in.
        Guid[] expected = [first.Id, second.Id, third.Id];

        Assert.That(ids, Is.EqualTo(expected), "the identifier breaks the tie so the order is total");
    }

    [Test]
    public async Task NothingButTheDateAndTheIdentifierOrdersTheList()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var sameDay = new DateOnly(2026, 8, 22);

        // A later act numbered below an earlier one: only the identifier can tell the two apart.
        var first = await this.tenant.AddAct(@case, sameDay, "Podání", actNumber: $"{@case.CaseNumber}/20260822-009");
        var second = await this.tenant.AddAct(@case, sameDay, "Výzva", actNumber: $"{@case.CaseNumber}/20260822-002");

        var ids = await this.tenant.Context.Acts.InListOrder().Select(act => act.Id).ToListAsync();

        Guid[] expected = [first.Id, second.Id];

        Assert.That(ids, Is.EqualTo(expected), "the date orders, and only the identifier breaks its ties");
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

        var ids = await this.tenant.Context.Acts.InListOrder().Select(act => act.Id).ToListAsync();

        Guid[] expected = [mine.Id];

        Assert.That(ids, Is.EqualTo(expected), "the tenant query filter is what keeps another tenant's rows out");
    }
}

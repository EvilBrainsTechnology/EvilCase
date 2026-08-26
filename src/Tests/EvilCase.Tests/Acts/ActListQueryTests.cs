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
    public async Task TheWriteMomentBreaksATieOnTheDate()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var sameDay = new DateOnly(2026, 8, 22);
        var actIds = TestTenant.SortedEntityIds(3);

        // The write order and the identifier order disagree, so only the write moment can break the tie.
        var first = await this.tenant.AddAct(@case, sameDay, "Podání", actId: actIds[2]);
        var second = await this.tenant.AddAct(@case, sameDay, "Výzva", actId: actIds[0]);
        var third = await this.tenant.AddAct(@case, sameDay, "Rozhodnutí", actId: actIds[1]);

        var ids = await this.tenant.Context.Acts.InListOrder().Select(act => act.Id).ToListAsync();

        Guid[] expected = [first.Id, second.Id, third.Id];

        Assert.That(ids, Is.EqualTo(expected), "equal act dates fall back to when the row was written");
    }

    [Test]
    public async Task NothingButTheDateAndTheWriteMomentOrdersTheList()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var sameDay = new DateOnly(2026, 8, 22);
        var actIds = TestTenant.SortedEntityIds(3);

        // The identifiers, the act numbers, the titles and the write order all disagree.
        var lowestNumber = await this.tenant.AddAct(@case, sameDay, "Rozhodnutí", actNumber: $"{@case.CaseNumber}/20260822-002", actId: actIds[2]);
        var highestNumber = await this.tenant.AddAct(@case, sameDay, "Výzva", actNumber: $"{@case.CaseNumber}/20260822-009", actId: actIds[1]);
        var middleNumber = await this.tenant.AddAct(@case, sameDay, "Podání", actNumber: $"{@case.CaseNumber}/20260822-005", actId: actIds[0]);

        var ids = await this.tenant.Context.Acts.InListOrder().Select(act => act.Id).ToListAsync();

        Guid[] expected = [lowestNumber.Id, highestNumber.Id, middleNumber.Id];

        Assert.That(ids, Is.EqualTo(expected), "the date orders, and only the write moment breaks its ties");
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

    [Test]
    public async Task OnlyTheActsOfTheCaseComeBack()
    {
        var first = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var second = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var inFirst = await this.tenant.AddAct(first, new DateOnly(2026, 8, 21), "Podání");
        await this.tenant.AddAct(second, new DateOnly(2026, 8, 21), "Jiný úkon");

        var ids = await this.tenant.Context.Acts.OfCase(first.Id).InListOrder().Select(act => act.Id).ToListAsync();

        Guid[] expected = [inFirst.Id];

        Assert.That(ids, Is.EqualTo(expected), "the act list of a case never reaches into another case");
    }

    [Test]
    public async Task AListItemCarriesBothContactNames()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var issuedBy = await this.tenant.AddContact("Městský úřad Vzorov");
        var addressedTo = await this.tenant.AddContact("Ing. Petr Vzorek");
        var act = await this.tenant.AddAct(@case, new DateOnly(2026, 8, 22), "Rozhodnutí", issuedBy: issuedBy, addressedTo: addressedTo);

        var item = await this.tenant.Context.Acts.OfCase(@case.Id).InListOrder().AsListItems().SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.IssuedByName, Is.EqualTo("Městský úřad Vzorov"));
            Assert.That(item.AddressedToName, Is.EqualTo("Ing. Petr Vzorek"));
            Assert.That(item.ActNumber, Is.EqualTo(act.ActNumber));
            Assert.That(item.Direction, Is.EqualTo(act.Direction));
            Assert.That(item.Title, Is.EqualTo(act.Title));
            Assert.That(item.Date, Is.EqualTo(act.Date));
        }
    }

    [Test]
    public async Task AListItemWithoutARecipientCarriesNoName()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        await this.tenant.AddAct(@case, new DateOnly(2026, 8, 22), "Podání", addressedTo: null);

        var item = await this.tenant.Context.Acts.OfCase(@case.Id).InListOrder().AsListItems().SingleAsync();

        Assert.That(item.AddressedToName, Is.Null, "a recipient is optional and its absence is a null name, not a failed read");
    }

    /// <summary>
    /// The database stamps <c>Created</c> off the clock, so two acts never share it and no result reaches
    /// the identifier behind it.
    /// </summary>
    [Test]
    public void TheIdentifierMakesTheOrderTotal()
    {
        var sql = this.tenant.Context.Acts.InListOrder().ToQueryString();

        var orderBy = sql.LastIndexOf("ORDER BY", StringComparison.Ordinal);

        Assert.That(orderBy, Is.GreaterThanOrEqualTo(0), "the list order is the database's");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql[orderBy..], Does.Contain("\"Created\""), "the write moment breaks a tie on the date");
            Assert.That(sql[orderBy..], Does.Contain("\"Id\""), "the identifier makes the order total");
        }
    }
}

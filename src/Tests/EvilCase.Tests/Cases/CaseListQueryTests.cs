using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseListQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheSearchFoldsCaseAndDiacriticsOverTheTitleAndTheDescription()
    {
        await this.Tenant.AddCase(Day, "Odvolání proti rozhodnutí");
        await this.Tenant.AddCase(Day, "Přestupek", description: "Odvolání podáno v termínu");
        await this.Tenant.AddCase(Day, "ODVOLANI bez diakritiky");
        await this.Tenant.AddCase(Day, "Nahlédnutí do spisu", description: "bez poznámky");

        var byPlainTerm = await this.Titles("odvolani");
        var byAccentedTerm = await this.Titles("Odvolání");

        string[] expected = ["Odvolání proti rozhodnutí", "Přestupek", "ODVOLANI bez diakritiky"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byPlainTerm, Is.EquivalentTo(expected), "the search folds case and diacritics over both the title and the description");
            Assert.That(byAccentedTerm, Is.EquivalentTo(expected), "the term folds too, so an accented term reaches a row written without diacritics");
        }
    }

    [Test]
    public async Task ABlankSearchReturnsEveryCaseOfTheTenant()
    {
        await this.Tenant.AddCase(Day, "Odvolání");
        await this.Tenant.AddCase(Day, "Přestupek");

        var unset = await this.Tenant.Context.Cases.MatchingSearch(search: null).CountAsync();
        var empty = await this.Tenant.Context.Cases.MatchingSearch("").CountAsync();
        var blank = await this.Tenant.Context.Cases.MatchingSearch("   ").CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unset, Is.EqualTo(2), "a blank term narrows nothing");
            Assert.That(empty, Is.EqualTo(2), "a blank term narrows nothing");
            Assert.That(blank, Is.EqualTo(2), "a blank term narrows nothing");
        }
    }

    [Test]
    public async Task AWildcardInTheTermMatchesOnlyItself()
    {
        await this.Tenant.AddCase(Day, @"Sleva 50%_a\b");
        await this.Tenant.AddCase(Day, "Sleva 50 ab");

        var titles = await this.Titles(@"50%_a\b");

        string[] expected = [@"Sleva 50%_a\b"];

        Assert.That(titles, Is.EqualTo(expected), "a wildcard in the term matches only itself");
    }

    [Test]
    public async Task TheOrderIsTheCasesOwnDateNewestFirstWithCreatedBreakingATie()
    {
        var caseIds = TestTenant.SortedEntityIds(2);
        var older = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "Starší datum");

        // The tie falls to the row written later even where the identifier alone would put it last.
        var written = await this.Tenant.AddCase(new DateOnly(2026, 8, 22), "Zapsáno dřív", caseId: caseIds[1]);
        var writtenLater = await this.Tenant.AddCase(new DateOnly(2026, 8, 22), "Zapsáno později", caseId: caseIds[0]);

        var ordered = await this.Tenant.Context.Cases.InListOrder().Select(static @case => @case.Id).ToListAsync();

        Guid[] expected = [writtenLater.Id, written.Id, older.Id];

        Assert.That(ordered, Is.EqualTo(expected), "the case's own date orders newest first and the write breaks a tie on it");
    }

    [Test]
    public async Task OpenIsEverythingNotClosedClosedIsOnlyTheClosedAndAllIsEverything()
    {
        var active = await this.Tenant.AddCase(Day, "Aktivní", status: CaseStatus.Active);
        var waiting = await this.Tenant.AddCase(Day, "Čeká na úřad", status: CaseStatus.WaitingOnAuthority);
        var closed = await this.Tenant.AddCase(Day, "Uzavřená", status: CaseStatus.Closed);

        var open = await this.IdsWithStatus(CaseStatusFilter.Open);
        var onlyClosed = await this.IdsWithStatus(CaseStatusFilter.Closed);
        var all = await this.IdsWithStatus(CaseStatusFilter.All);

        Guid[] expectedOpen = [active.Id, waiting.Id];
        Guid[] expectedClosed = [closed.Id];
        Guid[] expectedAll = [active.Id, waiting.Id, closed.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new CaseListRequest().Status, Is.EqualTo(CaseStatusFilter.Open), "the list opens on everything that is not closed");
            Assert.That(open, Is.EquivalentTo(expectedOpen), "open is everything not closed");
            Assert.That(onlyClosed, Is.EquivalentTo(expectedClosed), "closed is only the closed ones");
            Assert.That(all, Is.EquivalentTo(expectedAll), "all narrows nothing");
        }
    }

    [Test]
    public async Task TheSearchAndTheStatusNarrowTheSameQuery()
    {
        await this.Tenant.AddCase(Day, "Odvolání živé", status: CaseStatus.Active);
        var wanted = await this.Tenant.AddCase(Day, "Odvolání uzavřené", status: CaseStatus.Closed);
        await this.Tenant.AddCase(Day, "Přestupek", status: CaseStatus.Closed);

        var ids = await this.Tenant.Context.Cases
            .MatchingSearch("odvolani")
            .WithStatus(CaseStatusFilter.Closed)
            .InListOrder()
            .AsListItems()
            .Select(static item => item.CaseId)
            .ToListAsync();

        Guid[] expected = [wanted.Id];

        Assert.That(ids, Is.EqualTo(expected), "the two narrow together, not one instead of the other");
    }

    [Test]
    public async Task ARowCarriesTheCaseNumberTheTitleTheDateAndTheStatus()
    {
        var seeded = await this.Tenant.AddCase(new DateOnly(2026, 8, 21), "Přestupek", status: CaseStatus.WaitingOnAuthority);

        var row = await this.Tenant.Context.Cases.AsListItems().SingleAsync();

        var expected = new CaseListItem
        {
            CaseId = seeded.Id,
            CaseNumber = seeded.CaseNumber,
            Title = "Přestupek",
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.WaitingOnAuthority,
            Changed = seeded.Created,
        };

        Assert.That(row, Is.EqualTo(expected), "a row of the list shows the case's number, title, date and status");
    }

    [Test]
    public async Task ACaseOfAnotherTenantNeverComesBack()
    {
        var mine = await this.Tenant.AddCase(Day, "Moje věc");

        await using (var other = await TestTenant.Create())
            await other.AddCase(Day, "Cizí věc");

        var ids = await this.Tenant.Context.Cases
            .MatchingSearch(search: null)
            .WithStatus(CaseStatusFilter.All)
            .InListOrder()
            .AsListItems()
            .Select(static item => item.CaseId)
            .ToListAsync();

        Guid[] expected = [mine.Id];

        Assert.That(ids, Is.EqualTo(expected), "the tenant query filter is what keeps another tenant's rows out");
    }

    [Test]
    public void TheListReadsNoDescriptionCountsNothingAndPagesNothing()
    {
        var sql = this.Tenant.Context.Cases
            .MatchingSearch(search: null)
            .WithStatus(CaseStatusFilter.All)
            .InListOrder()
            .AsListItems()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Not.Contain("\"Description\""), "a row of the list never carries the case's text");
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "a row of the list stands for one case and counts nothing under it");
            Assert.That(sql, Does.Not.Contain("LIMIT"), "the list is not paged");
            Assert.That(sql, Does.Not.Contain("OFFSET"), "the list is not paged");
        }
    }

    /// <summary>
    /// The database stamps <c>Created</c> off the clock, so two rows never share it and no result reaches
    /// the identifier behind it.
    /// </summary>
    [Test]
    public void TheIdentifierMakesTheOrderTotal()
    {
        var sql = this.Tenant.Context.Cases.InListOrder().ToQueryString();

        Assert.That(sql, Does.Contain("\"Id\" DESC"), "the identifier makes the order total");
    }

    [Test]
    public async Task TheChangeOrderPutsTheLastChangedCaseFirst()
    {
        var a = await this.Tenant.AddCase(Day, "A");
        var b = await this.Tenant.AddCase(Day, "B");
        var c = await this.Tenant.AddCase(Day, "C");

        await this.Tenant.Context.Cases.Where(@case => @case.Id == a.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static @case => @case.Title, "A upravené"));
        await this.Tenant.Context.Cases.Where(@case => @case.Id == b.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static @case => @case.Title, "B upravené"));

        var ids = await this.Tenant.Context.Cases.InChangeOrder().Select(static @case => @case.Id).ToListAsync();

        Guid[] expected = [b.Id, a.Id, c.Id];

        Assert.That(ids, Is.EqualTo(expected), "the case's own Updated orders the list, and a case never edited falls back to its Created");
    }

    [Test]
    public async Task ARowCarriesWhenTheCaseLastChanged()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");

        var beforeEdit = await this.Tenant.Context.Cases.AsListItems().SingleAsync();

        Assert.That(beforeEdit.Changed, Is.EqualTo(seeded.Created), "a row shows the case's own last change, its Created while it has never been edited");

        await this.Tenant.Context.Cases.Where(@case => @case.Id == seeded.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static @case => @case.Title, "Přestupek upravený"));

        var afterEdit = await this.Tenant.Context.Cases.AsListItems().SingleAsync();
        var updated = await this.Tenant.Context.Cases.WithId(seeded.Id).Select(static @case => @case.Updated).SingleAsync();

        Assert.That(afterEdit.Changed, Is.EqualTo(updated), "a row shows the case's own last change, its Updated once it has been edited");
    }

    [Test]
    public async Task TheCapReturnsOnlyTheFirstCases()
    {
        var cases = new List<Case>();

        for (var day = 15; day <= 21; day++)
            cases.Add(await this.Tenant.AddCase(new DateOnly(2026, 8, day), $"Případ {day.ToString(CultureInfo.InvariantCulture)}"));

        var ids = await this.Tenant.Context.Cases.InListOrder().TakeAtMost(5).Select(static @case => @case.Id).ToListAsync();

        var expected = cases.TakeLast(5).Reverse().Select(static @case => @case.Id);

        Assert.That(ids, Is.EqualTo(expected), "the dashboard tile's five is a cap the database applies");
    }

    [Test]
    public async Task TheRequestedOrderIsTheOneTheListComesBackIn()
    {
        var older = await this.Tenant.AddCase(new DateOnly(2026, 8, 24), "Starší");
        var newer = await this.Tenant.AddCase(new DateOnly(2026, 8, 26), "Novější");

        await this.Tenant.Context.Cases.Where(@case => @case.Id == older.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static @case => @case.Title, "Starší upravený"));

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        var byDate = await reader.ListCases(new CaseListRequest(), CancellationToken.None);
        var byChange = await reader.ListCases(new CaseListRequest { Order = CaseListOrder.Changed }, CancellationToken.None);

        Guid[] byDateExpected = [newer.Id, older.Id];
        Guid[] byChangeExpected = [older.Id, newer.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byDate.Select(static item => item.CaseId), Is.EqualTo(byDateExpected), "the case's own date orders the list until the request asks for another order");
            Assert.That(byChange.Select(static item => item.CaseId), Is.EqualTo(byChangeExpected), "the requested order is the one the list comes back in");
        }
    }

    [Test]
    public async Task TheRequestedCapIsTheOneTheListComesBackWith()
    {
        for (var day = 15; day <= 21; day++)
            await this.Tenant.AddCase(new DateOnly(2026, 8, day), $"Případ {day.ToString(CultureInfo.InvariantCulture)}");

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));

        var capped = await reader.ListCases(new CaseListRequest { Take = 5 }, CancellationToken.None);
        var whole = await reader.ListCases(new CaseListRequest(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(capped, Has.Count.EqualTo(5), "the requested cap is the one the list comes back with");
            Assert.That(whole, Has.Count.EqualTo(7), "an absent cap narrows nothing");
        }
    }

    private async Task<List<string>> Titles(string search)
    {
        return await this.Tenant.Context.Cases
            .MatchingSearch(search)
            .Select(static @case => @case.Title)
            .ToListAsync();
    }

    private async Task<List<Guid>> IdsWithStatus(CaseStatusFilter filter)
    {
        return await this.Tenant.Context.Cases.WithStatus(filter).Select(static @case => @case.Id).ToListAsync();
    }
}

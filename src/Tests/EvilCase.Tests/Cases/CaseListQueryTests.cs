using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The list rules on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself. Only what a result cannot show is read off the generated SQL.
/// </summary>
public class CaseListQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 24);

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
    public async Task TheSearchFoldsCaseAndDiacriticsOverTheTitleAndTheDescription()
    {
        await this.tenant.AddCase(Day, "Odvolání proti rozhodnutí");
        await this.tenant.AddCase(Day, "Přestupek", description: "Odvolání podáno v termínu");
        await this.tenant.AddCase(Day, "Nahlédnutí do spisu", description: "bez poznámky");

        var titles = await this.tenant.Context.Cases.MatchingSearch("odvolani").Select(@case => @case.Title).ToListAsync();

        string[] expected = ["Odvolání proti rozhodnutí", "Přestupek"];

        Assert.That(titles, Is.EquivalentTo(expected), "the search folds case and diacritics over both the title and the description");
    }

    [Test]
    public async Task ABlankSearchReturnsEveryCaseOfTheTenant()
    {
        await this.tenant.AddCase(Day, "Odvolání");
        await this.tenant.AddCase(Day, "Přestupek");

        var unset = await this.tenant.Context.Cases.MatchingSearch(search: null).CountAsync();
        var empty = await this.tenant.Context.Cases.MatchingSearch("").CountAsync();
        var blank = await this.tenant.Context.Cases.MatchingSearch("   ").CountAsync();

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
        await this.tenant.AddCase(Day, @"Sleva 50%_a\b");
        await this.tenant.AddCase(Day, "Sleva 50 ab");

        var titles = await this.tenant.Context.Cases.MatchingSearch(@"50%_a\b").Select(@case => @case.Title).ToListAsync();

        string[] expected = [@"Sleva 50%_a\b"];

        Assert.That(titles, Is.EqualTo(expected), "a wildcard in the term matches only itself");
    }

    [Test]
    public async Task TheOrderIsTheCasesOwnDateNewestFirstWithCreatedBreakingATie()
    {
        var older = await this.tenant.AddCase(new DateOnly(2026, 8, 20), "Starší datum");
        var written = await this.tenant.AddCase(new DateOnly(2026, 8, 22), "Zapsáno dřív");
        var writtenLater = await this.tenant.AddCase(new DateOnly(2026, 8, 22), "Zapsáno později");

        var ids = await this.tenant.Context.Cases.InListOrder().Select(@case => @case.Id).ToListAsync();

        Guid[] expected = [writtenLater.Id, written.Id, older.Id];

        Assert.That(ids, Is.EqualTo(expected), "the case's own date orders newest first and the write breaks a tie on it");
    }

    [Test]
    public async Task OpenIsEverythingNotClosedClosedIsOnlyTheClosedAndAllIsEverything()
    {
        var active = await this.tenant.AddCase(Day, "Aktivní", status: CaseStatus.Active);
        var waiting = await this.tenant.AddCase(Day, "Čeká na úřad", status: CaseStatus.WaitingOnAuthority);
        var closed = await this.tenant.AddCase(Day, "Uzavřená", status: CaseStatus.Closed);

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
        await this.tenant.AddCase(Day, "Odvolání živé", status: CaseStatus.Active);
        var wanted = await this.tenant.AddCase(Day, "Odvolání uzavřené", status: CaseStatus.Closed);
        await this.tenant.AddCase(Day, "Přestupek", status: CaseStatus.Closed);

        var ids = await this.tenant.Context.Cases
            .MatchingSearch("odvolani")
            .WithStatus(CaseStatusFilter.Closed)
            .InListOrder()
            .AsListItems()
            .Select(item => item.Id)
            .ToListAsync();

        Guid[] expected = [wanted.Id];

        Assert.That(ids, Is.EqualTo(expected), "the two narrow together, not one instead of the other");
    }

    [Test]
    public async Task ARowCarriesTheCaseNumberTheTitleTheDateAndTheStatus()
    {
        var seeded = await this.tenant.AddCase(new DateOnly(2026, 8, 21), "Přestupek", status: CaseStatus.WaitingOnAuthority);

        var row = await this.tenant.Context.Cases.AsListItems().SingleAsync();

        var expected = new CaseListItem
        {
            Id = seeded.Id,
            CaseNumber = seeded.CaseNumber,
            Title = "Přestupek",
            Date = new DateOnly(2026, 8, 21),
            Status = CaseStatus.WaitingOnAuthority,
        };

        Assert.That(row, Is.EqualTo(expected), "a row of the list shows the case's number, title, date and status");
    }

    [Test]
    public async Task ACaseOfAnotherTenantNeverComesBack()
    {
        var mine = await this.tenant.AddCase(Day, "Moje věc");

        await using (var other = await TestTenant.Create())
            await other.AddCase(Day, "Cizí věc");

        var ids = await this.tenant.Context.Cases
            .MatchingSearch(search: null)
            .WithStatus(CaseStatusFilter.All)
            .InListOrder()
            .AsListItems()
            .Select(item => item.Id)
            .ToListAsync();

        Guid[] expected = [mine.Id];

        Assert.That(ids, Is.EqualTo(expected), "the tenant query filter is what keeps another tenant's rows out");
    }

    /// <summary>
    /// What a returned row cannot show.
    /// </summary>
    [Test]
    public void TheListReadsNoDescriptionCountsNothingAndPagesNothing()
    {
        var sql = this.tenant.Context.Cases
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
        var sql = this.tenant.Context.Cases.InListOrder().ToQueryString();

        Assert.That(sql, Does.Contain("\"Id\" DESC"), "the identifier makes the order total");
    }

    private async Task<List<Guid>> IdsWithStatus(CaseStatusFilter filter)
    {
        return await this.tenant.Context.Cases.WithStatus(filter).Select(@case => @case.Id).ToListAsync();
    }
}

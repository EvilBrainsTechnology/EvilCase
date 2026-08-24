using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The list rules on the rows a real PostgreSQL returns. Each test seeds its own tenant and the query
/// filter keeps the rows apart, so no test cleans up after itself. Only what a result cannot show is
/// read off the generated SQL.
/// </summary>
public class CaseListQueryTests
{
    private readonly StubUserContext userContext = new();

    private Guid tenantId;

    private Guid userId;

    private int seeded;

    private IDisposable entered = null!;

    private ApplicationDbContext context = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenantId = Guid.CreateVersion7();
        this.userId = Guid.CreateVersion7();
        this.seeded = 0;
        this.entered = this.userContext.Enter(this.tenantId, this.userId);
        this.context = TestDatabase.CreateMigrated(this.userContext);

        await SeedTenant(this.context, this.tenantId, this.userId);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.context.DisposeAsync();
        this.entered.Dispose();
    }

    [Test]
    public async Task TheSearchFoldsCaseAndDiacriticsOverTheTitleAndTheDescription()
    {
        await this.SeedCase("Odvolání proti rozhodnutí");
        await this.SeedCase("Přestupek", description: "Odvolání podáno v termínu");
        await this.SeedCase("Nahlédnutí do spisu", description: "bez poznámky");

        var titles = await this.context.Cases.MatchingSearch("odvolani").Select(@case => @case.Title).ToListAsync();

        string[] expected = ["Odvolání proti rozhodnutí", "Přestupek"];

        Assert.That(titles, Is.EquivalentTo(expected));
    }

    [Test]
    public async Task ABlankSearchReturnsEveryCaseOfTheTenant()
    {
        await this.SeedCase("Odvolání");
        await this.SeedCase("Přestupek");

        var unset = await this.context.Cases.MatchingSearch(search: null).CountAsync();
        var empty = await this.context.Cases.MatchingSearch("").CountAsync();
        var blank = await this.context.Cases.MatchingSearch("   ").CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unset, Is.EqualTo(2));
            Assert.That(empty, Is.EqualTo(2));
            Assert.That(blank, Is.EqualTo(2));
        }
    }

    [Test]
    public async Task AWildcardInTheTermMatchesOnlyItself()
    {
        await this.SeedCase(@"Sleva 50%_a\b");
        await this.SeedCase("Sleva 50 ab");

        var titles = await this.context.Cases.MatchingSearch(@"50%_a\b").Select(@case => @case.Title).ToListAsync();

        string[] expected = [@"Sleva 50%_a\b"];

        Assert.That(
            titles,
            Is.EqualTo(expected),
            @"unescaped, the term would read as a pattern and take ""Sleva 50 ab"" with it");
    }

    [Test]
    public async Task TheOrderIsTheCasesOwnDateNewestFirstWithCreatedBreakingATie()
    {
        var older = await this.SeedCase("Starší datum", date: new DateOnly(2026, 8, 20));
        var written = await this.SeedCase("Zapsáno dřív", date: new DateOnly(2026, 8, 22));
        var writtenLater = await this.SeedCase("Zapsáno později", date: new DateOnly(2026, 8, 22));

        var ids = await this.context.Cases.InListOrder().Select(@case => @case.Id).ToListAsync();

        Guid[] expected = [writtenLater.Id, written.Id, older.Id];

        Assert.That(ids, Is.EqualTo(expected));
    }

    [Test]
    public async Task OpenIsEverythingNotClosedClosedIsOnlyTheClosedAndAllIsEverything()
    {
        var active = await this.SeedCase("Aktivní", status: CaseStatus.Active);
        var waiting = await this.SeedCase("Čeká na úřad", status: CaseStatus.WaitingOnAuthority);
        var closed = await this.SeedCase("Uzavřená", status: CaseStatus.Closed);

        var open = await this.IdsWithStatus(CaseStatusFilter.Open);
        var onlyClosed = await this.IdsWithStatus(CaseStatusFilter.Closed);
        var all = await this.IdsWithStatus(CaseStatusFilter.All);

        Guid[] expectedOpen = [active.Id, waiting.Id];
        Guid[] expectedClosed = [closed.Id];
        Guid[] expectedAll = [active.Id, waiting.Id, closed.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new CaseListRequest().Status, Is.EqualTo(CaseStatusFilter.Open), "the list opens on everything that is not closed");
            Assert.That(open, Is.EquivalentTo(expectedOpen));
            Assert.That(onlyClosed, Is.EquivalentTo(expectedClosed));
            Assert.That(all, Is.EquivalentTo(expectedAll));
        }
    }

    [Test]
    public async Task TheSearchAndTheStatusNarrowTheSameQuery()
    {
        await this.SeedCase("Odvolání živé", status: CaseStatus.Active);
        var wanted = await this.SeedCase("Odvolání uzavřené", status: CaseStatus.Closed);
        await this.SeedCase("Přestupek", status: CaseStatus.Closed);

        var ids = await this.context.Cases
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
    public async Task ACaseOfAnotherTenantNeverComesBack()
    {
        var mine = await this.SeedCase("Moje věc");
        await SeedCaseInAnotherTenant();

        var ids = await this.context.Cases
            .MatchingSearch(search: null)
            .WithStatus(CaseStatusFilter.All)
            .InListOrder()
            .AsListItems()
            .Select(item => item.Id)
            .ToListAsync();

        Guid[] expected = [mine.Id];

        Assert.That(ids, Is.EqualTo(expected));
    }

    /// <summary>
    /// The three rules a returned row cannot show.
    /// </summary>
    [Test]
    public void TheListReadsNoDescriptionCountsNothingAndPagesNothing()
    {
        var sql = this.context.Cases
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
    /// the identifier that makes the order total.
    /// </summary>
    [Test]
    public void TheIdentifierMakesTheOrderTotal()
    {
        var sql = this.context.Cases.InListOrder().ToQueryString();

        Assert.That(sql, Does.Contain("\"Id\" DESC"));
    }

    private static async Task SeedTenant(ApplicationDbContext dbContext, Guid tenant, Guid user)
    {
        var account = new Account { Name = "case list" };
        var defaultContact = new Contact { TenantId = tenant, Kind = ContactKind.Person, Name = "default" };

        dbContext.Accounts.Add(account);
        dbContext.Tenants.Add(new Tenant { Id = tenant, AccountId = account.Id, Name = "tenant" });
        dbContext.Contacts.Add(defaultContact);
        dbContext.Users.Add(new User
        {
            Id = user,
            TenantId = tenant,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = defaultContact.Id,
        });

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedCaseInAnotherTenant()
    {
        var otherUserContext = new StubUserContext();
        var otherTenantId = Guid.CreateVersion7();
        var otherUserId = Guid.CreateVersion7();
        using var otherEntered = otherUserContext.Enter(otherTenantId, otherUserId);

        await using var otherContext = TestDatabase.CreateMigrated(otherUserContext);
        await SeedTenant(otherContext, otherTenantId, otherUserId);

        otherContext.Cases.Add(new Case
        {
            TenantId = otherTenantId,
            UserId = otherUserId,
            CaseNumber = "EC/20260824-001",
            Date = new DateOnly(2026, 8, 24),
            Title = "Cizí věc",
            Status = CaseStatus.Active,
        });

        await otherContext.SaveChangesAsync();
    }

    /// <summary>
    /// One case, in its own save — the database stamps <c>Created</c> off the clock, so a later save
    /// carries a later stamp.
    /// </summary>
    private async Task<Case> SeedCase(
        string title,
        string? description = null,
        DateOnly? date = null,
        CaseStatus status = CaseStatus.Active)
    {
        this.seeded++;

        var @case = new Case
        {
            TenantId = this.tenantId,
            UserId = this.userId,
            CaseNumber = $"EC/20260824-{this.seeded.ToString("D3", CultureInfo.InvariantCulture)}",
            Date = date ?? new DateOnly(2026, 8, 24),
            Title = title,
            Description = description,
            Status = status,
        };

        this.context.Cases.Add(@case);
        await this.context.SaveChangesAsync();

        return @case;
    }

    private async Task<List<Guid>> IdsWithStatus(CaseStatusFilter filter)
    {
        return await this.context.Cases.WithStatus(filter).Select(@case => @case.Id).ToListAsync();
    }
}

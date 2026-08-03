using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Reads the SQL each step of the list query produces, without a server: the design-time factory names
/// no connection string and <c>ToQueryString</c> opens none. What is pinned is that the narrowing happens
/// in PostgreSQL rather than in the application, which is invisible from the outside until the table is
/// big enough for it to matter.
/// </summary>
public class CaseListQueryTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheListIsRootsOnly()
    {
        var sql = this.context.Cases.Roots().ToQueryString();

        Assert.That(sql, Does.Contain("\"ParentCaseId\" IS NULL"));
    }

    [Test]
    public void SearchMatchesTheTitleAndTheSubjectWithoutRegardToCase()
    {
        var sql = this.context.Cases.MatchingSearch("odvolání").ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ILIKE"), "case folding belongs in the database, not in a ToLower() the index cannot use");
            Assert.That(sql, Does.Contain("\"Title\""));
            Assert.That(sql, Does.Contain("\"Subject\""));
            Assert.That(sql, Does.Contain("%odvolání%"));
        }
    }

    [Test]
    public void ABlankSearchNarrowsNothing()
    {
        var unfiltered = this.context.Cases.ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(this.context.Cases.MatchingSearch(search: null).ToQueryString(), Is.EqualTo(unfiltered));
            Assert.That(this.context.Cases.MatchingSearch("").ToQueryString(), Is.EqualTo(unfiltered));
            Assert.That(this.context.Cases.MatchingSearch("   ").ToQueryString(), Is.EqualTo(unfiltered));
        }
    }

    /// <summary>
    /// A term is text the user typed, not a pattern. Without escaping, a case titled "sleva 50%" would be
    /// found by typing "%" — and so would every other case.
    /// </summary>
    [Test]
    public void WildcardsInTheTermAreEscaped()
    {
        var sql = this.context.Cases.MatchingSearch("50%_a\\b").ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain(@"%50\%\_a\\b%"));
            Assert.That(sql, Does.Contain("ESCAPE"));
        }
    }

    /// <summary>
    /// The default is the one worth pinning: a request that names no filter must not quietly show the
    /// archive (#100), and only <c>All</c> may leave the query untouched.
    /// </summary>
    [Test]
    public void OpenIsEverythingNotClosedAndOnlyAllNarrowsNothing()
    {
        var unfiltered = this.context.Cases.ToQueryString();
        var open = this.context.Cases.WithStatus(CaseStatusFilter.Open).ToQueryString();
        var closed = this.context.Cases.WithStatus(CaseStatusFilter.Closed).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new CaseListRequest().Status, Is.EqualTo(CaseStatusFilter.Open), "a request that says nothing gets the open cases");
            Assert.That(this.context.Cases.WithStatus(CaseStatusFilter.All).ToQueryString(), Is.EqualTo(unfiltered));
            Assert.That(open, Does.Contain("<>"), "open is everything not closed, so a status added later is open without a code change");
            Assert.That(open, Does.Contain(nameof(CaseStatus.Closed)));
            Assert.That(closed, Does.Contain("\"Status\""));
            Assert.That(closed, Does.Contain(nameof(CaseStatus.Closed)), "the status is stored as its name, so the parameter carries the name too");
        }
    }

    /// <summary>
    /// Two cases changed in the same instant must not swap places between two calls, which an order on
    /// the date alone allows.
    /// </summary>
    [Test]
    public void TheOrderIsWhatWasTouchedLastAndIsTotal()
    {
        var sql = this.context.Cases.InListOrder().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ORDER BY"));
            Assert.That(sql, Does.Contain("COALESCE"), "a case never changed is ordered by when it was founded");
            Assert.That(sql, Does.Contain("\"Id\" DESC"));
        }
    }

    /// <summary>
    /// The projection is what decides which columns are read. Selecting the entity and shaping afterwards
    /// would fetch every column of every case and one query per row for the tags.
    /// </summary>
    [Test]
    public void TheProjectionReadsTheTagsAndCountsTheSubCasesInTheSameQuery()
    {
        var sql = this.context.Cases.AsListItems().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("count(*)").IgnoreCase, "the sub-case count is a count, not the children themselves");
            Assert.That(sql, Does.Contain("\"CaseTags\""));
            Assert.That(sql, Does.Not.Contain("\"OwnerId\""), "a column no row shows is a column the list does not read");
        }
    }
}

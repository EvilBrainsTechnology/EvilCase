using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Data.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
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
            Assert.That(sql, Does.Contain("ILIKE"));
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

    [Test]
    public void OpenIsEverythingNotClosedAndOnlyAllNarrowsNothing()
    {
        var unfiltered = this.context.Cases.ToQueryString();
        var open = this.context.Cases.WithStatus(CaseStatusFilter.Open).ToQueryString();
        var closed = this.context.Cases.WithStatus(CaseStatusFilter.Closed).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new CaseListRequest().Status, Is.EqualTo(CaseStatusFilter.Open));
            Assert.That(this.context.Cases.WithStatus(CaseStatusFilter.All).ToQueryString(), Is.EqualTo(unfiltered));
            Assert.That(open, Does.Contain("<>"), "open is everything not closed");
            Assert.That(open, Does.Contain(nameof(CaseStatus.Closed)));
            Assert.That(closed, Does.Contain("\"Status\""));
            Assert.That(closed, Does.Contain(nameof(CaseStatus.Closed)), "the status is stored as its name");
        }
    }

    [Test]
    public void TheOrderIsWhatWasTouchedLastAndIsTotal()
    {
        var sql = this.context.Cases.InListOrder().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ORDER BY"));
            Assert.That(sql, Does.Contain("COALESCE"));
            Assert.That(sql, Does.Contain("\"Id\" DESC"));
        }
    }

    [Test]
    public void TheProjectionReadsTheTagsAndCountsTheSubCasesInTheSameQuery()
    {
        var sql = this.context.Cases.AsListItems().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("count(*)").IgnoreCase);
            Assert.That(sql, Does.Contain("\"CaseTags\""));
            Assert.That(sql, Does.Not.Contain("\"OwnerId\""));
        }
    }
}

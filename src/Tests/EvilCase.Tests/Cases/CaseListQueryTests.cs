using EvilBrains.EvilCase.Business.Cases;
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
    public void SearchMatchesTheTitleAndTheDescriptionWithoutRegardToCaseOrDiacritics()
    {
        var sql = this.context.Cases.MatchingSearch("odvolání").ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ILIKE"));
            Assert.That(sql, Does.Contain("\"Title\""));
            Assert.That(sql, Does.Contain("\"Description\""));
            Assert.That(sql, Does.Contain("%odvolání%"));
            Assert.That(sql, Does.Contain("immutable_unaccent"), "the fold runs in the database, over the wrapper the Init migration creates");
            Assert.That(sql.Split("immutable_unaccent").Length - 1, Is.EqualTo(4), "both the column and the term fold on both comparisons");
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
    public void TheOrderIsTheCasesOwnDateNewestFirst()
    {
        var sql = this.context.Cases.InListOrder().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ORDER BY"));
            Assert.That(sql, Does.Contain("\"Date\" DESC"));
            Assert.That(sql, Does.Contain("\"Created\" DESC"));
            Assert.That(sql, Does.Contain("\"Id\" DESC"), "the identifier makes the order total");
        }
    }

    [Test]
    public void TheProjectionReadsOnlyWhatARowShows()
    {
        var sql = this.context.Cases.AsListItems().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Contain("\"Title\""));
            Assert.That(sql, Does.Contain("\"Status\""));
            Assert.That(sql, Does.Contain("\"Date\""));
            Assert.That(sql, Does.Not.Contain("\"Description\""), "a row of the list never carries the case's text");
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "a row of the list stands for one case and counts nothing under it");
        }
    }
}

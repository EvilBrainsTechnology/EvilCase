using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class ContactListQueryTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void SearchMatchesTheNameAndTheDataBoxIdWithoutRegardToCaseOrDiacritics()
    {
        var sql = this.context.Contacts.MatchingSearch("úřad").ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ILIKE"));
            Assert.That(sql, Does.Contain("\"Name\""));
            Assert.That(sql, Does.Contain("\"DataBoxId\""));
            Assert.That(sql, Does.Contain("%úřad%"));
            Assert.That(sql, Does.Contain("immutable_unaccent"), "the fold runs in the database, over the wrapper the Init migration creates");
            Assert.That(sql.Split("immutable_unaccent").Length - 1, Is.EqualTo(4), "both the column and the term fold on both comparisons");
        }
    }

    [Test]
    public void ABlankSearchNarrowsNothing()
    {
        var unfiltered = this.context.Contacts.ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(this.context.Contacts.MatchingSearch(search: null).ToQueryString(), Is.EqualTo(unfiltered));
            Assert.That(this.context.Contacts.MatchingSearch("").ToQueryString(), Is.EqualTo(unfiltered));
            Assert.That(this.context.Contacts.MatchingSearch("   ").ToQueryString(), Is.EqualTo(unfiltered));
        }
    }

    [Test]
    public void WildcardsInTheTermAreEscaped()
    {
        var sql = this.context.Contacts.MatchingSearch("50%_a\\b").ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain(@"%50\%\_a\\b%"));
            Assert.That(sql, Does.Contain("ESCAPE"));
        }
    }

    [Test]
    public void TheOrderIsByNameAndIsTotal()
    {
        var sql = this.context.Contacts.InListOrder().ToQueryString();

        Assert.That(sql, Does.Contain("ORDER BY c.\"Name\", c.\"Id\""), "the identifier only breaks a tie on the name");
    }

    [Test]
    public void TheProjectionReadsWhatARowShows()
    {
        var sql = this.context.Contacts.AsListItems().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Name\""));
            Assert.That(sql, Does.Contain("\"Kind\""));
            Assert.That(sql, Does.Contain("\"DataBoxId\""));
            Assert.That(sql, Does.Contain("\"Address\""));
            Assert.That(sql, Does.Not.Contain("\"Created\""), "a row of the list shows no timestamp");
        }
    }

    [Test]
    public void TheListIsNarrowedByTheTenantAloneAndShowsEveryContact()
    {
        var sql = this.context.Contacts.MatchingSearch(search: null).InListOrder().AsListItems().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"TenantId\""), "every read is inside a tenant");
            Assert.That(sql, Does.Not.Contain("ILIKE"), "a blank search narrows nothing");
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "a row of the list stands for one contact and counts nothing under it");
            Assert.That(sql, Does.Not.Contain("LIMIT"), "the overview has no paging");
        }
    }
}

using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class NumberQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    private static readonly DateOnly ActDay = new(2026, 8, 12);

    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheDaysCaseNumbersAreReadByTheirPrefix()
    {
        var sql = this.context.Cases.WithNumberOfDay(Day).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Contain("LIKE"));
            Assert.That(sql, Does.Contain("EC/20260807-%"));
            Assert.That(sql, Does.Contain("\"TenantId\""), "every read is inside a tenant");
        }
    }

    [Test]
    public void TheDayComesFromTheNumberAndNotTheCaseDate()
    {
        var sql = this.context.Cases.WithNumberOfDay(Day).ToQueryString();

        Assert.That(sql, Does.Not.Contain("\"Date\""), "the day of the mark and the date of the case are not the same thing");
    }

    [Test]
    public void AnActsDayIsReadByTheCaseNumberAndTheDay()
    {
        var sql = this.context.Acts.WithNumberOfDay("EC/20260807-001", ActDay).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("EC/20260807-001/20260812-%"));
            Assert.That(sql, Does.Not.Contain("\"CaseId\""), "a re-issued case number must not make two cases share a sequence");
        }
    }

    [Test]
    public void AWildcardInAHandWrittenCaseNumberIsALiteral()
    {
        var sql = this.context.Acts.WithNumberOfDay("spis 100%_a", ActDay).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ESCAPE"));
            Assert.That(sql, Does.Contain(@"spis 100\%\_a"));
        }
    }
}

using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class ActNumberQueryTests
{
    private static readonly Guid CaseId = Guid.Parse("0199f0a0-0000-7000-8000-000000000001", CultureInfo.InvariantCulture);

    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheDaysNumbersAreNarrowedByTheCaseAndThePrefix()
    {
        var prefix = ActNumberFormat.Prefix("EC/20260807-001", new DateOnly(2026, 8, 12));
        var sql = this.context.Acts.OfCaseWithNumberPrefix(CaseId, prefix).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseId\""));
            Assert.That(sql, Does.Contain("EC/20260807-001/20260812-%"));
            Assert.That(sql, Does.Contain("LIKE"));
            Assert.That(sql, Does.Contain("ESCAPE"));
        }
    }

    [Test]
    public void WildcardsInAHandWrittenCaseNumberAreEscaped()
    {
        var prefix = ActNumberFormat.Prefix("EC/100%_1", new DateOnly(2026, 8, 12));
        var sql = this.context.Acts.OfCaseWithNumberPrefix(CaseId, prefix).ToQueryString();

        Assert.That(sql, Does.Contain(@"EC/100\%\_1/20260812-%"), "a wildcard in a hand-written case number matches only itself");
    }

    [Test]
    public void TheHighestNumberIsTheOneRowTheDatabasePicks()
    {
        var sql = this.context.Acts.HighestNumber().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ORDER BY"));
            Assert.That(sql, Does.Contain("DESC"));
            Assert.That(sql, Does.Contain("LIMIT"));
            Assert.That(sql, Does.Contain("length").IgnoreCase);
            Assert.That(sql, Does.Contain("\"ActNumber\""));
        }
    }
}

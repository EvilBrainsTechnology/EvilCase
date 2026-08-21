using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Numbering;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class CaseNumberQueryTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheDaysNumbersAreNarrowedByTheirPrefix()
    {
        var sql = this.context.Cases.WithNumberPrefix(CaseNumberFormat.Prefix(new DateOnly(2026, 8, 7))).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("LIKE"));
            Assert.That(sql, Does.Contain("EC/20260807-%"));
            Assert.That(sql, Does.Contain("ESCAPE"));
            Assert.That(sql, Does.Contain("\"TenantId\""), "every read is inside a tenant");
        }
    }

    [Test]
    public void TheCaseNumberOrderIsDescendingAndTheStepTakesNoRow()
    {
        var sql = this.context.Cases.OrderByNumberDescending().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("ORDER BY"));
            Assert.That(sql, Does.Contain("DESC"));
            Assert.That(sql, Does.Contain("length").IgnoreCase);
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Not.Contain("LIMIT"), "the step orders and stops, the caller takes the row");
        }
    }
}

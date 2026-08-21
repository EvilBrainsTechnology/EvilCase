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
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheDaysCaseNumbersAreFoundByTheDayPrefix()
    {
        var sql = this.context.Cases.CaseNumbersOfDay(new DateOnly(2026, 8, 7)).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("LIKE"));
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Contain("EC/20260807-%"));
        }
    }

    [Test]
    public void TheCasesActNumbersAreFoundByTheCaseAlone()
    {
        var sql = this.context.Acts.ActNumbersOfCase(Guid.CreateVersion7()).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseId\""));
            Assert.That(sql, Does.Contain("\"ActNumber\""));
            Assert.That(sql, Does.Not.Contain("LIKE"), "the day is counted in memory, so a rewritten case number keeps counting");
        }
    }

    [Test]
    public void AnExistingCaseNumberIsFound()
    {
        var sql = this.context.Cases.WithCaseNumber("EC/20260807-001", excluding: null).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Contain("EC/20260807-001"));
            Assert.That(sql, Does.Not.Contain("<>"));
        }
    }

    [Test]
    public void TheCaseBeingEditedIsLeftOut()
    {
        var sql = this.context.Cases.WithCaseNumber("EC/20260807-001", excluding: Guid.CreateVersion7()).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Id\""));
            Assert.That(sql, Does.Contain("<>"));
        }
    }

    [Test]
    public void AnExistingActNumberIsFoundAndTheActBeingEditedIsLeftOut()
    {
        var found = this.context.Acts.WithActNumber("EC/20260807-001/20260812-001", excluding: null).ToQueryString();
        var excluding = this.context.Acts.WithActNumber("EC/20260807-001/20260812-001", excluding: Guid.CreateVersion7()).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(found, Does.Contain("\"ActNumber\""));
            Assert.That(found, Does.Contain("EC/20260807-001/20260812-001"));
            Assert.That(found, Does.Not.Contain("<>"));
            Assert.That(excluding, Does.Contain("\"Id\""));
            Assert.That(excluding, Does.Contain("<>"));
        }
    }
}

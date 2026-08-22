using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class CaseDetailQueryTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp()
    {
        this.context = new ApplicationDbContextFactory().CreateDbContext([]);
    }

    [TearDown]
    public void TearDown()
    {
        this.context.Dispose();
    }

    [Test]
    public void TheProjectionReadsOnlyWhatTheDetailShows()
    {
        var sql = this.context.Cases.AsDetails().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Contain("\"Date\""));
            Assert.That(sql, Does.Contain("\"Title\""));
            Assert.That(sql, Does.Contain("\"Description\""));
            Assert.That(sql, Does.Contain("\"Status\""));
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "the detail counts nothing under the case");
            Assert.That(sql, Does.Not.Contain("JOIN").IgnoreCase, "the detail reads the case row and nothing else");
        }
    }

    [Test]
    public void TheDetailIsOneRowPickedByItsId()
    {
        var sql = CaseReader.Compose(this.context.Cases, Guid.Empty).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Id\""));
            Assert.That(sql, Does.Contain("WHERE"));
        }
    }
}

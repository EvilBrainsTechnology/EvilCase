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
            Assert.That(sql, Does.Not.Contain("\"Created\""), "the detail shows no timestamp");
            Assert.That(sql, Does.Not.Contain("\"Updated\""), "the detail shows no timestamp");
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "the detail counts nothing under the case");
            Assert.That(sql, Does.Not.Contain("JOIN").IgnoreCase, "the detail reads the case row and nothing else");
        }
    }

    [Test]
    public void TheDetailIsOneRowPickedByItsIdWithinTheTenant()
    {
        var sql = CaseReader.Compose(this.context.Cases, Guid.Empty).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Id\" = @id"), "the detail is picked by the id from the route");
            Assert.That(sql, Does.Contain("\"TenantId\" ="), "an id of another tenant names no case");
        }
    }
}

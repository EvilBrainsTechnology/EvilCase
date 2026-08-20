using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// <see cref="CaseListQueryTests"/> pins each step on its own; this pins which of them the list runs,
/// reading the SQL off the reader itself.
/// </summary>
public class CaseReaderTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheTenantIsTheOnlyThingThatNarrowsAnUnfilteredList()
    {
        var sql = CaseReader.Compose(this.context, new CaseListRequest { Status = CaseStatusFilter.All }).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"TenantId\""));
            Assert.That(sql, Does.Not.Contain("ILIKE"), "the blank search narrows nothing");
            Assert.That(sql, Does.Not.Contain(" AND "), "the tenant filter is the only condition — All narrows nothing further");
            Assert.That(sql, Does.Not.Contain("\"CaseRelations\""));
        }
    }

    [Test]
    public void TheReaderRunsTheSearchAndTheStatusTheRequestAsksFor()
    {
        var sql = CaseReader.Compose(this.context, new CaseListRequest { Search = "odvolání" }).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("%odvolání%"), "the reader runs the request's search");
            Assert.That(sql, Does.Contain(nameof(CaseStatus.Closed)), "the reader runs the request's status, open by default");
            Assert.That(sql, Does.Contain("ORDER BY COALESCE("), "the list's own order leads");
            Assert.That(sql, Does.Contain("\"Id\" DESC"), "the identifier breaks the tie");
        }
    }
}

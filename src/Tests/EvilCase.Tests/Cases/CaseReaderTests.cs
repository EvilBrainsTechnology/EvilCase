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

    /// <summary>
    /// Related cases are cases, and the list shows every one of them — no row stands for a set of others.
    /// </summary>
    [Test]
    public void OnlyTheSearchAndTheStatusNarrowTheList()
    {
        var sql = CaseReader.Compose(this.context, new CaseListRequest { Status = CaseStatusFilter.All }).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Not.Contain("WHERE"), "nothing but the search and the status hides a case from the list");
            Assert.That(sql, Does.Not.Contain("\"CaseRelations\""), "a related case is a row of the list like any other");
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
            Assert.That(sql, Does.Contain("ORDER BY"), "the list has an order");
            Assert.That(sql, Does.Contain("\"CaseTags\""), "a row carries its tags");
        }
    }
}

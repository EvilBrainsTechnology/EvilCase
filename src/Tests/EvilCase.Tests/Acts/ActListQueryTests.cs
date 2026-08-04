using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// Reads the SQL each step produces, without a server — <c>ToQueryString</c> opens no connection.
/// </summary>
public class ActListQueryTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheListIsOneCase()
    {
        var sql = this.context.Acts.OfCase(7).ToQueryString();

        var where = sql[sql.LastIndexOf("WHERE", StringComparison.Ordinal)..];

        Assert.That(where, Does.Contain("\"CaseId\""), "an act list reads the acts of one case");
    }

    [Test]
    public void TheOrderIsTheActDateAlone()
    {
        var sql = this.context.Acts.InListOrder().ToQueryString();

        var order = sql[sql.LastIndexOf("ORDER BY", StringComparison.Ordinal)..];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(order, Does.Contain("\"Date\""), "act lists are ordered by the act date");
            Assert.That(order, Does.Not.Contain("DESC"), "a case file reads oldest first");
            Assert.That(order.Split(','), Has.Length.EqualTo(2), "the date orders, and only the identifier breaks its ties");
            Assert.That(order, Does.Contain("\"Id\""), "the identifier breaks the tie so the order is total");
        }
    }
}

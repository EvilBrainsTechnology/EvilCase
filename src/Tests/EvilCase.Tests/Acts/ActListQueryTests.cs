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
    public void TheOrderIsTheActDateAlone()
    {
        var sql = this.context.Acts.InListOrder().ToQueryString();

        var order = sql[sql.LastIndexOf("ORDER BY", StringComparison.Ordinal)..].Trim();
        var keys = order["ORDER BY".Length..].Split(',').Select(key => key.Trim()).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(keys, Has.Count.EqualTo(2), "the date orders, and only the identifier breaks its ties");
            Assert.That(keys[0], Does.Contain("\"Date\"").And.Not.Contain("DESC"), "act lists are ordered by the act date, oldest first");
            Assert.That(keys[1], Does.Contain("\"Id\"").And.Not.Contain("DESC"), "the identifier breaks the tie so the order is total");
        }
    }
}

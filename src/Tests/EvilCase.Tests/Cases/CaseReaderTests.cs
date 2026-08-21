using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Reads the SQL <see cref="CaseReader.Compose"/> really runs, without a server.
/// </summary>
public class CaseReaderTests
{
    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void TheListShowsEveryCaseInDateOrderAndPagesNothing()
    {
        var sql = CaseReader.Compose(this.context).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Date\" DESC"));
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Not.Contain("\"Status\" ="), "nothing but the tenant hides a case from the list");
            Assert.That(sql, Does.Not.Contain("LIMIT"), "the list is not paged");
            Assert.That(sql, Does.Not.Contain("OFFSET"), "the list is not paged");
        }
    }
}

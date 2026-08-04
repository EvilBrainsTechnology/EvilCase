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
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void OneCaseIsMatchedByItsIdentifier()
    {
        var sql = this.context.Cases.WithId(42).ToQueryString();

        Assert.That(sql, Does.Contain("\"Id\" = "));
    }

    [Test]
    public void TheTreeWalkRunsBothWaysInOneRecursiveQuery()
    {
        var sql = this.context.Cases.AroundCase(42).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("WITH RECURSIVE"));
            Assert.That(sql, Does.Contain("\"Ancestry\""), "the walk goes up to the root");
            Assert.That(sql, Does.Contain("\"SubTree\""), "and down to every leaf");
            Assert.That(sql, Does.Contain("\"ParentCaseId\""), "nesting is the self-reference");
        }
    }

    /// <summary>
    /// A parent chain that closed a cycle would otherwise never stop.
    /// </summary>
    [Test]
    public void TheTreeWalkIsBoundedByADistance()
    {
        var sql = this.context.Cases.AroundCase(42).ToQueryString();

        Assert.That(sql, Does.Contain("\"Distance\""));
    }

    [Test]
    public void TheTreeProjectionReadsOnlyWhatARowShows()
    {
        var sql = this.context.Cases.AroundCase(42).AsGraphNodes().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseNumber\""));
            Assert.That(sql, Does.Contain("\"ParentCaseId\""), "the nesting is built from it");
            Assert.That(sql, Does.Not.Contain("\"Subject\""), "a tree row shows no subject");
        }
    }

    [Test]
    public void TheDetailReadsTheTagsInTheSameQuery()
    {
        var sql = this.context.Cases.WithId(42).AsDetails().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseTags\""));
            Assert.That(sql, Does.Contain("\"Subject\""));
            Assert.That(sql, Does.Not.Contain("\"OwnerId\""));
        }
    }

    [Test]
    public void TheThreadIsTheCasesOwnNotesNewestFirst()
    {
        var sql = this.context.Comments.OnCase(42).InDiaryOrder().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"CaseId\" = "));
            Assert.That(sql, Does.Contain("\"Created\" DESC"));
            Assert.That(sql, Does.Contain("\"Id\" DESC"), "the identifier makes the order total");
        }
    }

    [Test]
    public void TheCommentProjectionNamesItsAuthorInTheSameQuery()
    {
        var sql = this.context.Comments.OnCase(42).AsCaseComments().ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("\"Users\""));
            Assert.That(sql, Does.Contain("\"Email\""));
            Assert.That(sql, Does.Not.Contain("\"ActId\""), "a case comment says nothing about acts");
        }
    }
}

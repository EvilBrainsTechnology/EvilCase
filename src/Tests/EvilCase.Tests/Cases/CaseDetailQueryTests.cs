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
    /// A parent chain that closed a cycle would otherwise never stop. Nothing here proves it does — only
    /// <see cref="CaseWalkDatabaseTests"/>, against a server, does.
    /// </summary>
    [Test]
    public void EachBranchOfTheTreeWalkStopsOnACaseItHasAlreadyWalked()
    {
        var sql = this.context.Cases.AroundCase(42).ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("NOT parent.\"Id\" = ANY (child.\"Walked\")"), "the walk up stops on a case it has already been to");
            Assert.That(sql, Does.Contain("NOT child.\"Id\" = ANY (parent.\"Walked\")"), "and so does the walk down");
        }
    }

    /// <summary>
    /// Whatever bounds the walk, it is not how deep a case nests.
    /// </summary>
    [Test]
    public void NoDistanceBoundsHowDeepTheTreeWalkGoes()
    {
        var sql = this.context.Cases.AroundCase(42).ToQueryString();

        Assert.That(sql, Does.Not.Contain("\"Distance\""), "a distance would truncate a chain instead of walking it");
    }

    [Test]
    public void TheTreeProjectionReadsOnlyWhatARowShows()
    {
        var sql = this.context.Cases.AroundCase(42).AsGraphNodes().ToQueryString();

        var projection = sql[..sql.IndexOf("FROM (", StringComparison.Ordinal)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(projection, Does.Contain("\"CaseNumber\""));
            Assert.That(projection, Does.Contain("\"ParentCaseId\""), "the nesting is built from it");
            Assert.That(projection, Does.Not.Contain("\"Subject\""), "a tree row shows no subject");
            Assert.That(projection, Does.Not.Contain("\"Created\""), "nor when the case was opened");
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

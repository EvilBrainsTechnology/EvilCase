using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Comments;

/// <summary>
/// Reads a case's comments, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class CommentReaderTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private CommentReader reader = null!;

    [SetUp]
    public void SetUpReader()
    {
        this.reader = new CommentReader(new FixedDbSession(this.Tenant.Context), this.Tenant.UserContext);
    }

    [Test]
    public async Task ANoteCarriesItsBodyItsAuthorAndItsStamp()
    {
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddCaseComment(@case, "Poznámka");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);
        var item = items.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(item.Body, Is.EqualTo("Poznámka"));
            Assert.That(item.AuthorEmail, Does.Contain("@example.com"), "the seeded user's e-mail is what the join carries");
            Assert.That(item.IsAuthor, Is.True, "the signed-in user wrote it");
            Assert.That(item.Updated, Is.Null, "an unedited note carries no stamp");
            Assert.That(item.Created, Is.Not.Default, "the write is what stamps Created");
        }
    }

    [Test]
    public async Task TheOrderIsOldestFirst()
    {
        var @case = await this.Tenant.AddCase(Day);
        var first = await this.Tenant.AddCaseComment(@case, "První");
        var second = await this.Tenant.AddCaseComment(@case, "Druhá");
        var third = await this.Tenant.AddCaseComment(@case, "Třetí");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        Assert.That(items.Select(static item => item.CommentId), Is.EqualTo([first.Id, second.Id, third.Id]), "the diary reads oldest first");
    }

    [Test]
    public async Task AnotherUsersNoteIsListedButNotAsTheSignedInUsers()
    {
        var @case = await this.Tenant.AddCase(Day);
        var other = await this.Tenant.AddUser();
        var theirs = await this.Tenant.AddCaseComment(@case, "Jejich", other.Id);
        var ours = await this.Tenant.AddCaseComment(@case, "Naše");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Select(static item => item.CommentId), Is.EquivalentTo([theirs.Id, ours.Id]), "both notes come back");
            Assert.That(items.Single(item => item.CommentId == theirs.Id).IsAuthor, Is.False);
            Assert.That(items.Single(item => item.CommentId == theirs.Id).AuthorEmail, Is.EqualTo(other.Email));
            Assert.That(items.Single(item => item.CommentId == ours.Id).IsAuthor, Is.True);
        }
    }

    [Test]
    public async Task ANoteOfAnotherCaseNeverComesBack()
    {
        var first = await this.Tenant.AddCase(Day, "První");
        var second = await this.Tenant.AddCase(Day, "Druhý");
        var ownComment = await this.Tenant.AddCaseComment(first, "Vlastní");
        await this.Tenant.AddCaseComment(second, "Cizí");

        var items = await this.reader.ListCaseComments(first.Id, CancellationToken.None);

        Assert.That(items.Select(static item => item.CommentId), Is.EqualTo([ownComment.Id]), "the list holds only the asked case's notes");
    }

    [Test]
    public async Task ANoteOfAnActIsNotACaseNote()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        await this.Tenant.AddActComment(act, "Poznámka k úkonu");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "a note on an act is not a note on its case");
    }

    [Test]
    public async Task ANoteOfAnotherTenantNeverComesBack()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        await other.AddCaseComment(otherCase, "Cizí");
        var @case = await this.Tenant.AddCase(Day);

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "the tenant query filter is what turns another tenant's note into nothing");
    }

    /// <summary>
    /// The database stamps <c>Created</c> off the clock, so two notes never share it and no result reaches
    /// the identifier behind it.
    /// </summary>
    [Test]
    public void TheIdentifierMakesTheDiaryOrderTotal()
    {
        var context = this.Tenant.Context;

        var sql = context.Comments
            .OnCase(Guid.CreateVersion7())
            .AsCommentItems(context.Users, this.Tenant.UserId)
            .InDiaryOrder()
            .ToQueryString();

        var orderBy = sql.LastIndexOf("ORDER BY", StringComparison.Ordinal);

        Assert.That(orderBy, Is.GreaterThanOrEqualTo(0), "the diary order is the database's");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql[orderBy..], Does.Contain("\"Created\""), "the stamp reads oldest first");
            Assert.That(sql[orderBy..], Does.Contain("\"Id\""), "the identifier makes the order total");
        }
    }
}

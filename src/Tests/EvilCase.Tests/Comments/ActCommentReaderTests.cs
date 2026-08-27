using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Comments;

/// <summary>
/// Reads an act's comments, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class ActCommentReaderTests : TenantFixture
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
        var act = await this.Tenant.AddAct(@case, Day);
        await this.Tenant.AddActComment(act, "Poznámka");

        var items = await this.reader.ListActComments(@case.Id, act.Id, CancellationToken.None);
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
        var act = await this.Tenant.AddAct(@case, Day);
        var first = await this.Tenant.AddActComment(act, "První");
        var second = await this.Tenant.AddActComment(act, "Druhá");
        var third = await this.Tenant.AddActComment(act, "Třetí");

        var items = await this.reader.ListActComments(@case.Id, act.Id, CancellationToken.None);

        Assert.That(items.Select(item => item.CommentId), Is.EqualTo([first.Id, second.Id, third.Id]), "the diary reads oldest first");
    }

    [Test]
    public async Task AnotherUsersNoteIsListedButNotAsTheSignedInUsers()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var other = await this.Tenant.AddUser();
        var theirs = await this.Tenant.AddActComment(act, "Jejich", other.Id);
        var ours = await this.Tenant.AddActComment(act, "Naše");

        var items = await this.reader.ListActComments(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Select(item => item.CommentId), Is.EquivalentTo([theirs.Id, ours.Id]), "both notes come back");
            Assert.That(items.Single(item => item.CommentId == theirs.Id).IsAuthor, Is.False);
            Assert.That(items.Single(item => item.CommentId == theirs.Id).AuthorEmail, Is.EqualTo(other.Email));
            Assert.That(items.Single(item => item.CommentId == ours.Id).IsAuthor, Is.True);
        }
    }

    [Test]
    public async Task ANoteOfAnotherActNeverComesBack()
    {
        var @case = await this.Tenant.AddCase(Day);
        var first = await this.Tenant.AddAct(@case, Day, "První");
        var second = await this.Tenant.AddAct(@case, Day, "Druhý");
        var ownComment = await this.Tenant.AddActComment(first, "Vlastní");
        await this.Tenant.AddActComment(second, "Cizí");

        var items = await this.reader.ListActComments(@case.Id, first.Id, CancellationToken.None);

        Assert.That(items.Select(item => item.CommentId), Is.EqualTo([ownComment.Id]), "the list holds only the asked act's notes");
    }

    [Test]
    public async Task ANoteOfTheCaseIsNotAnActNote()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        await this.Tenant.AddCaseComment(@case, "Poznámka ke spisu");

        var items = await this.reader.ListActComments(@case.Id, act.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "a note on the case is not a note on its act");
    }

    [Test]
    public async Task AnActReadUnderAnotherCaseNeverComesBack()
    {
        var @case = await this.Tenant.AddCase(Day, "První");
        var otherCase = await this.Tenant.AddCase(Day, "Druhý");
        var act = await this.Tenant.AddAct(@case, Day);
        await this.Tenant.AddActComment(act, "Poznámka");

        var items = await this.reader.ListActComments(otherCase.Id, act.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "the act must hang on the asked case");
    }

    [Test]
    public async Task ANoteOfAnotherTenantNeverComesBack()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        await other.AddActComment(otherAct, "Cizí");
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var items = await this.reader.ListActComments(@case.Id, act.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "the tenant query filter is what turns another tenant's note into nothing");
    }
}

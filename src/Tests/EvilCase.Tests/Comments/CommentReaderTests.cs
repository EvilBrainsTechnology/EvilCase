using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Comments;

/// <summary>
/// Reads a case's comments, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class CommentReaderTests
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private TestTenant tenant = null!;

    private CommentReader reader = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.reader = new CommentReader(new FixedDbSession(this.tenant.Context), this.tenant.UserContext);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task ANoteCarriesItsBodyItsAuthorAndItsStamp()
    {
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddCaseComment(@case, "Poznámka");

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
        var @case = await this.tenant.AddCase(Day);
        var first = await this.tenant.AddCaseComment(@case, "První");
        var second = await this.tenant.AddCaseComment(@case, "Druhá");
        var third = await this.tenant.AddCaseComment(@case, "Třetí");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        Assert.That(items.Select(item => item.Id), Is.EqualTo([first.Id, second.Id, third.Id]), "the diary reads oldest first");
    }

    [Test]
    public async Task AnotherUsersNoteIsListedButNotAsTheSignedInUsers()
    {
        var @case = await this.tenant.AddCase(Day);
        var other = await this.tenant.AddUser("kolega@example.com");
        var theirs = await this.tenant.AddCaseComment(@case, "Jejich", other.Id);
        var ours = await this.tenant.AddCaseComment(@case, "Naše");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Select(item => item.Id), Is.EquivalentTo([theirs.Id, ours.Id]), "both notes come back");
            Assert.That(items.Single(item => item.Id == theirs.Id).IsAuthor, Is.False);
            Assert.That(items.Single(item => item.Id == theirs.Id).AuthorEmail, Is.EqualTo("kolega@example.com"));
            Assert.That(items.Single(item => item.Id == ours.Id).IsAuthor, Is.True);
        }
    }

    [Test]
    public async Task ANoteOfAnotherCaseNeverComesBack()
    {
        var first = await this.tenant.AddCase(Day, "První");
        var second = await this.tenant.AddCase(Day, "Druhý");
        var ownComment = await this.tenant.AddCaseComment(first, "Vlastní");
        await this.tenant.AddCaseComment(second, "Cizí");

        var items = await this.reader.ListCaseComments(first.Id, CancellationToken.None);

        Assert.That(items.Select(item => item.Id), Is.EqualTo([ownComment.Id]), "the list holds only the asked case's notes");
    }

    [Test]
    public async Task ANoteOfAnActIsNotACaseNote()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);
        await this.tenant.AddActComment(act, "Poznámka k úkonu");

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "a note on an act is not a note on its case");
    }

    [Test]
    public async Task ANoteOfAnotherTenantNeverComesBack()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        await other.AddCaseComment(otherCase, "Cizí");
        var @case = await this.tenant.AddCase(Day);

        var items = await this.reader.ListCaseComments(@case.Id, CancellationToken.None);

        Assert.That(items, Is.Empty, "the tenant query filter is what turns another tenant's note into nothing");
    }
}

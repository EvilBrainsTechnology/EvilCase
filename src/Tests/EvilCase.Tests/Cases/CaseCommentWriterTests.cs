using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The one write the case detail carries, against a server: it reaches for a case, so nothing without one
/// would see which cases it is allowed to reach. Ignored where no PostgreSQL answers.
/// </summary>
public class CaseCommentWriterTests
{
    private static readonly DateTime Opened = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Written = new(2026, 8, 4, 9, 30, 0, DateTimeKind.Utc);

    private readonly FakeOwnerContext owner = new() { OwnerId = 0 };

    private ApplicationDbContext? context;

    private long mine;

    private long theirs;

    [OneTimeSetUp]
    public async Task SetUp()
    {
        this.context = TestDatabase.Create("comments");

        if (this.context is null)
            return;

        this.owner.OwnerId = await this.SeedUser("mine@evilcase.test");
        var other = await this.SeedUser("theirs@evilcase.test");

        this.mine = await this.SeedCase(this.owner.OwnerId, "EC-MINE-001");
        this.theirs = await this.SeedCase(other, "EC-THEIRS-001");
    }

    [OneTimeTearDown]
    public async Task TearDown()
    {
        if (this.context is null)
            return;

        await this.context.Database.EnsureDeletedAsync();
        await this.context.DisposeAsync();
    }

    [SetUp]
    public void RequireDatabase()
    {
        if (this.context is null)
            Assert.Ignore("no PostgreSQL answered on EVILCASE_TESTS_POSTGRES, so a write cannot be run against a server");
    }

    [Test]
    public async Task ACaseOfAnotherOwnerIsNoCaseAtAll()
    {
        var written = await this.Writer().Add(this.theirs, new AddCaseCommentRequest { Body = "cizí spis" });

        var comments = await this.context!.Comments.CountAsync(comment => comment.CaseId == this.theirs);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(written, Is.Null, "a write never reaches outside the caller's own cases");
            Assert.That(comments, Is.Zero, "and leaves nothing behind when it is refused");
        }
    }

    [Test]
    public async Task TheCallersOwnCaseTakesTheComment()
    {
        var written = await this.Writer().Add(this.mine, new AddCaseCommentRequest { Body = "  zápis  " });

        Assert.That(written?.Body, Is.EqualTo("zápis"), "the body is stored trimmed");
    }

    /// <summary>
    /// Decision #175: the list answers "where was I", so writing the diary moves the case up it.
    /// </summary>
    [Test]
    public async Task WritingACommentMovesTheCaseUpTheList()
    {
        var fresh = await this.SeedCase(this.owner.OwnerId, "EC-MINE-002");

        Assert.That(await this.Updated(fresh), Is.Null, "a case nothing has touched carries no Updated");

        _ = await this.Writer().Add(fresh, new AddCaseCommentRequest { Body = "posouvá spis" });

        Assert.That(await this.Updated(fresh), Is.EqualTo(Written), "a comment bumps the case's Updated");
    }

    [Test]
    public async Task ARefusedWriteLeavesTheCaseWhereItWas()
    {
        var before = await this.Updated(this.theirs);

        _ = await this.Writer().Add(this.theirs, new AddCaseCommentRequest { Body = "cizí spis" });

        Assert.That(await this.Updated(this.theirs), Is.EqualTo(before), "another owner's case does not move either");
    }

    private CaseCommentWriter Writer() =>
        new(this.context!, this.owner, new TestTimeProvider(Written));

    private async Task<DateTime?> Updated(long caseId) => await this.context!.Cases
        .Where(@case => @case.Id == caseId)
        .Select(@case => @case.Updated)
        .SingleAsync();

    private async Task<long> SeedUser(string email)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = "not-a-hash",
            Role = UserRole.User,
            Created = Opened,
        };

        _ = this.context!.Users.Add(user);
        _ = await this.context.SaveChangesAsync();

        return user.Id;
    }

    private async Task<long> SeedCase(long ownerId, string caseNumber)
    {
        var @case = new Case
        {
            OwnerId = ownerId,
            CaseNumber = caseNumber,
            Title = caseNumber,
            Status = CaseStatus.Active,
            Created = Opened,
        };

        _ = this.context!.Cases.Add(@case);
        _ = await this.context.SaveChangesAsync();

        return @case.Id;
    }
}

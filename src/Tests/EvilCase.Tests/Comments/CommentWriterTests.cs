using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Comments;

public class CommentWriterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private CommentWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new CommentWriter(new FixedDbSession(this.Tenant.Context), this.Tenant.UserContext, NullLogger<CommentWriter>.Instance);
    }

    [Test]
    public async Task ANoteIsFiledOnTheCaseUnderItsAuthor()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.AddCaseComment(@case.Id, new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.Written));
            Assert.That(reloaded.Body, Is.EqualTo("Poznámka"));
            Assert.That(reloaded.CaseId, Is.EqualTo(@case.Id));
            Assert.That(reloaded.ActId, Is.Null, "a case note carries no act");
            Assert.That(reloaded.UserId, Is.EqualTo(this.Tenant.UserId), "the write interceptor stamps the author");
            Assert.That(reloaded.TenantId, Is.Not.EqualTo(Guid.Empty), "the write interceptor stamps the tenant");
        }
    }

    [Test]
    public async Task ABodyIsStoredWithoutItsSurroundingSpace()
    {
        var @case = await this.Tenant.AddCase(Day);

        await this.writer.AddCaseComment(@case.Id, new CommentEditRequest { Body = "  text  " }, CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        Assert.That(reloaded.Body, Is.EqualTo("text"));
    }

    [Test]
    public async Task AnUnknownCaseTakesNoNote()
    {
        var outcome = await this.writer.AddCaseComment(Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var any = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(any, Is.False, "nothing is written when the case does not exist");
        }
    }

    [Test]
    public async Task ACaseOfAnotherTenantTakesNoNote()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);

        var outcome = await this.writer.AddCaseComment(otherCase.Id, new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var ownAny = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync();
        var otherAny = await other.Context.Comments.AsNoTracking().AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound), "the tenant query filter is what turns another tenant's case into nothing");
            Assert.That(ownAny, Is.False);
            Assert.That(otherAny, Is.False);
        }
    }

    [Test]
    public async Task TheAuthorEditsTheirOwnNote()
    {
        var @case = await this.Tenant.AddCase(Day);
        var comment = await this.Tenant.AddCaseComment(@case, "Původní");

        var outcome = await this.writer.UpdateCaseComment(@case.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.Written));
            Assert.That(reloaded.Body, Is.EqualTo("Upravená"));
            Assert.That(reloaded.Updated, Is.Not.Null, "the trigger stamps an edit");
        }
    }

    [Test]
    public async Task AnotherUsersNoteIsNotEdited()
    {
        var @case = await this.Tenant.AddCase(Day);
        var other = await this.Tenant.AddUser();
        var comment = await this.Tenant.AddCaseComment(@case, "Původní", other.Id);

        var outcome = await this.writer.UpdateCaseComment(@case.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotAuthor));
            Assert.That(reloaded.Body, Is.EqualTo("Původní"));
        }
    }

    [Test]
    public async Task AnUnknownNoteIsNotEdited()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.UpdateCaseComment(@case.Id, Guid.CreateVersion7(), new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
    }

    [Test]
    public async Task ANoteOfAnotherCaseIsNotEditedUnderThisCase()
    {
        var right = await this.Tenant.AddCase(Day, "Správný");
        var wrong = await this.Tenant.AddCase(Day, "Jiný");
        var comment = await this.Tenant.AddCaseComment(right, "Původní");

        var outcome = await this.writer.UpdateCaseComment(wrong.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Tenant.Context.Comments.AsNoTracking().SingleAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(reloaded.Body, Is.EqualTo("Původní"));
        }
    }

    [Test]
    public async Task TheAuthorDeletesTheirOwnNote()
    {
        var @case = await this.Tenant.AddCase(Day);
        var comment = await this.Tenant.AddCaseComment(@case, "Poznámka");

        var outcome = await this.writer.DeleteCaseComment(@case.Id, comment.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.Written));
            Assert.That(exists, Is.False);
        }
    }

    [Test]
    public async Task AnotherUsersNoteIsNotDeleted()
    {
        var @case = await this.Tenant.AddCase(Day);
        var other = await this.Tenant.AddUser();
        var comment = await this.Tenant.AddCaseComment(@case, "Poznámka", other.Id);

        var outcome = await this.writer.DeleteCaseComment(@case.Id, comment.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotAuthor));
            Assert.That(exists, Is.True);
        }
    }

    [Test]
    public async Task AnUnknownNoteIsNotDeleted()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.DeleteCaseComment(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
    }

    [Test]
    public async Task ANoteOfAnotherTenantIsNotDeleted()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var comment = await other.AddCaseComment(otherCase, "Poznámka");

        var outcome = await this.writer.DeleteCaseComment(otherCase.Id, comment.Id, CancellationToken.None);

        var exists = await other.Context.Comments.AsNoTracking().AnyAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound), "the tenant query filter is what turns another tenant's note into nothing");
            Assert.That(exists, Is.True, "the other tenant still holds it");
        }
    }

    private async Task<Comment> Reload(Guid caseId)
    {
        return await this.Tenant.Context.Comments.AsNoTracking().SingleAsync(comment => comment.CaseId == caseId);
    }
}

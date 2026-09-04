using EvilBrains.EvilCase.Api.Contract.Comments;
using EvilBrains.EvilCase.Business.Comments;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Comments;

public class ActCommentWriterTests : TenantFixture
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
    public async Task ANoteIsFiledOnTheActUnderItsAuthor()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var outcome = await this.writer.AddActComment(@case.Id, act.Id, new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.Written));
            Assert.That(reloaded.Body, Is.EqualTo("Poznámka"));
            Assert.That(reloaded.ActId, Is.EqualTo(act.Id));
            Assert.That(reloaded.CaseId, Is.Null, "an act note carries no case");
            Assert.That(reloaded.UserId, Is.EqualTo(this.Tenant.UserId), "the write interceptor stamps the author");
            Assert.That(reloaded.TenantId, Is.Not.EqualTo(Guid.Empty), "the write interceptor stamps the tenant");
        }
    }

    [Test]
    public async Task ABodyIsStoredWithoutItsSurroundingSpace()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        await this.writer.AddActComment(@case.Id, act.Id, new CommentEditRequest { Body = "  text  " }, CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

        Assert.That(reloaded.Body, Is.EqualTo("text"));
    }

    [Test]
    public async Task AnUnknownActTakesNoNote()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.AddActComment(@case.Id, Guid.CreateVersion7(), new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var any = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(any, Is.False, "nothing is written when the act does not exist");
        }
    }

    [Test]
    public async Task AnActOfAnotherCaseTakesNoNote()
    {
        var @case = await this.Tenant.AddCase(Day, "Správný");
        var otherCase = await this.Tenant.AddCase(Day, "Jiný");
        var act = await this.Tenant.AddAct(@case, Day);

        var outcome = await this.writer.AddActComment(otherCase.Id, act.Id, new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var any = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound), "the act must hang on the asked case");
            Assert.That(any, Is.False);
        }
    }

    [Test]
    public async Task AnActOfAnotherTenantTakesNoNote()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);

        var outcome = await this.writer.AddActComment(otherCase.Id, otherAct.Id, new CommentEditRequest { Body = "Poznámka" }, CancellationToken.None);

        var ownAny = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync();
        var otherAny = await other.Context.Comments.AsNoTracking().AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound), "the tenant query filter is what turns another tenant's act into nothing");
            Assert.That(ownAny, Is.False);
            Assert.That(otherAny, Is.False);
        }
    }

    [Test]
    public async Task TheAuthorEditsTheirOwnNote()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var comment = await this.Tenant.AddActComment(act, "Původní");

        var outcome = await this.writer.UpdateActComment(@case.Id, act.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

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
        var act = await this.Tenant.AddAct(@case, Day);
        var other = await this.Tenant.AddUser();
        var comment = await this.Tenant.AddActComment(act, "Původní", other.Id);

        var outcome = await this.writer.UpdateActComment(@case.Id, act.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

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
        var act = await this.Tenant.AddAct(@case, Day);

        var outcome = await this.writer.UpdateActComment(@case.Id, act.Id, Guid.CreateVersion7(), new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
    }

    [Test]
    public async Task ANoteOfAnotherActIsNotEditedUnderThisAct()
    {
        var @case = await this.Tenant.AddCase(Day);
        var right = await this.Tenant.AddAct(@case, Day, "Správný");
        var wrong = await this.Tenant.AddAct(@case, Day, "Jiný");
        var comment = await this.Tenant.AddActComment(right, "Původní");

        var outcome = await this.writer.UpdateActComment(@case.Id, wrong.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Tenant.Context.Comments.AsNoTracking().SingleAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(reloaded.Body, Is.EqualTo("Původní"));
        }
    }

    [Test]
    public async Task ANoteIsNotEditedUnderAnotherCase()
    {
        var @case = await this.Tenant.AddCase(Day, "Správný");
        var otherCase = await this.Tenant.AddCase(Day, "Jiný");
        var act = await this.Tenant.AddAct(@case, Day);
        var comment = await this.Tenant.AddActComment(act, "Původní");

        var outcome = await this.writer.UpdateActComment(otherCase.Id, act.Id, comment.Id, new CommentEditRequest { Body = "Upravená" }, CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(reloaded.Body, Is.EqualTo("Původní"), "the act must hang on the asked case");
        }
    }

    [Test]
    public async Task TheAuthorDeletesTheirOwnNote()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var comment = await this.Tenant.AddActComment(act, "Poznámka");

        var outcome = await this.writer.DeleteActComment(@case.Id, act.Id, comment.Id, CancellationToken.None);

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
        var act = await this.Tenant.AddAct(@case, Day);
        var other = await this.Tenant.AddUser();
        var comment = await this.Tenant.AddActComment(act, "Poznámka", other.Id);

        var outcome = await this.writer.DeleteActComment(@case.Id, act.Id, comment.Id, CancellationToken.None);

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
        var act = await this.Tenant.AddAct(@case, Day);

        var outcome = await this.writer.DeleteActComment(@case.Id, act.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
    }

    [Test]
    public async Task ANoteOfAnotherActIsNotDeletedUnderThisAct()
    {
        var @case = await this.Tenant.AddCase(Day);
        var right = await this.Tenant.AddAct(@case, Day, "Správný");
        var wrong = await this.Tenant.AddAct(@case, Day, "Jiný");
        var comment = await this.Tenant.AddActComment(right, "Poznámka");

        var outcome = await this.writer.DeleteActComment(@case.Id, wrong.Id, comment.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(exists, Is.True, "the note must hang on the act in the route");
        }
    }

    [Test]
    public async Task ANoteIsNotDeletedUnderAnotherCase()
    {
        var @case = await this.Tenant.AddCase(Day, "Správný");
        var otherCase = await this.Tenant.AddCase(Day, "Jiný");
        var act = await this.Tenant.AddAct(@case, Day);
        var comment = await this.Tenant.AddActComment(act, "Poznámka");

        var outcome = await this.writer.DeleteActComment(otherCase.Id, act.Id, comment.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.Comments.AsNoTracking().AnyAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound));
            Assert.That(exists, Is.True, "the act must hang on the asked case");
        }
    }

    [Test]
    public async Task ANoteOfAnotherTenantIsNotDeleted()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        var comment = await other.AddActComment(otherAct, "Poznámka");

        var outcome = await this.writer.DeleteActComment(otherCase.Id, otherAct.Id, comment.Id, CancellationToken.None);

        var exists = await other.Context.Comments.AsNoTracking().AnyAsync(c => c.Id == comment.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CommentWriteOutcome.NotFound), "the tenant query filter is what turns another tenant's note into nothing");
            Assert.That(exists, Is.True, "the other tenant still holds it");
        }
    }

    private async Task<Comment> Reload(Guid actId)
    {
        return await this.Tenant.Context.Comments.AsNoTracking().SingleAsync(comment => comment.ActId == actId);
    }
}

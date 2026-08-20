using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class CommentModelTests : ModelFixture
{
    [Test]
    public void ANoteHangsOnACaseOrAnActAndTheDatabaseHoldsThat()
    {
        var comment = DesignTimeModel.FindEntityType(typeof(Comment));

        Assert.That(comment, Is.Not.Null);

        var check = comment.GetCheckConstraints().SingleOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(check, Is.Not.Null, "the rule is in the database, not only in the code that writes a note");
            Assert.That(check?.Sql, Does.Contain("<>"), "exactly one parent — never both, never neither");
            Assert.That(comment.FindProperty(nameof(Comment.CaseId))?.IsNullable, Is.True);
            Assert.That(comment.FindProperty(nameof(Comment.ActId))?.IsNullable, Is.True);
            Assert.That(comment.FindProperty(nameof(Comment.UserId))?.IsNullable, Is.False);
            Assert.That(comment.FindProperty(nameof(Comment.Body))?.GetMaxLength(), Is.Null, "a note is as long as it needs to be");
        }
    }

    [Test]
    public void ANoteGoesWithWhateverItHangsOn()
    {
        var comment = Model.FindEntityType(typeof(Comment));

        Assert.That(comment, Is.Not.Null);

        var toCase = ForeignKeyTo<Case>(comment);
        var toAct = ForeignKeyTo<Act>(comment);
        var toUser = ForeignKeyTo<User>(comment);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(toAct?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(toUser?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict), "a note dies with its case or its act; the author outlives it");
        }
    }
}

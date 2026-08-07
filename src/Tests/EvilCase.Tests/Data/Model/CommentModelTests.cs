using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using static EvilBrains.EvilCase.Tests.Data.Model.ModelFixture;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class CommentModelTests
{
    [Test]
    public void ANoteHangsOnACaseOrAnActAndTheDatabaseHoldsThat()
    {
        var comment = DesignTime.FindEntityType(typeof(Comment));

        Assert.That(comment, Is.Not.Null);

        var check = comment.GetCheckConstraints().SingleOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(check, Is.Not.Null, "the rule is in the database, not only in the code that writes a note");
            Assert.That(check?.Sql, Does.Contain("<>"), "exactly one parent — never both, never neither");
            Assert.That(comment.FindProperty(nameof(Comment.CaseId))?.IsNullable, Is.True);
            Assert.That(comment.FindProperty(nameof(Comment.ActId))?.IsNullable, Is.True);
            Assert.That(comment.FindProperty(nameof(Comment.Body))?.GetMaxLength(), Is.Null, "a note is as long as it needs to be");
        }
    }

    [Test]
    public void ANoteGoesWithWhateverItHangsOn()
    {
        var comment = Runtime.FindEntityType(typeof(Comment));

        Assert.That(comment, Is.Not.Null);

        Assert.That(
            comment.GetForeignKeys().All(key => key.DeleteBehavior == DeleteBehavior.Cascade),
            Is.True,
            "a note has no meaning without its case, its act or its author");
    }
}

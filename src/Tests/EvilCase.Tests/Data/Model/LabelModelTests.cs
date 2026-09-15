using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class LabelModelTests : ModelFixture
{
    [Test]
    public void ALabelNamesItselfOnceInTheTenant()
    {
        var label = Model.FindEntityType(typeof(Label));

        Assert.That(label, Is.Not.Null);

        var unique = label.GetIndexes().SingleOrDefault(static index => index.IsUnique);

        string[] expected = [nameof(Label.TenantId), nameof(Label.Name)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                unique?.Properties.Select(static property => property.Name),
                Is.EqualTo(expected),
                "the unique index leads with the tenant, so two tenants may name a label the same");
            Assert.That(label.FindProperty(nameof(Label.Name))?.GetMaxLength(), Is.EqualTo(64));
            Assert.That(label.FindProperty(nameof(Label.Color))?.IsNullable, Is.False, "a label is always painted");
        }
    }

    [Test]
    public void AnAssignmentHangsOnACaseOrAnActAndTheDatabaseHoldsThat()
    {
        var assignment = DesignTimeModel.FindEntityType(typeof(LabelAssignment));

        Assert.That(assignment, Is.Not.Null);

        var check = assignment.GetCheckConstraints().SingleOrDefault();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(check, Is.Not.Null, "the rule is in the database, not only in the code that assigns a label");
            Assert.That(check?.Sql, Does.Contain("<>"), "exactly one owner — never both, never neither");
            Assert.That(assignment.FindProperty(nameof(LabelAssignment.CaseId))?.IsNullable, Is.True);
            Assert.That(assignment.FindProperty(nameof(LabelAssignment.ActId))?.IsNullable, Is.True);
        }
    }

    [Test]
    public void AnAssignmentGoesWithItsLabelAndWithWhateverItHangsOn()
    {
        var assignment = Model.FindEntityType(typeof(LabelAssignment));

        Assert.That(assignment, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ForeignKeyTo<Label>(assignment)?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "dropping a label takes it off everything that carried it");
            Assert.That(ForeignKeyTo<Case>(assignment)?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
            Assert.That(ForeignKeyTo<Act>(assignment)?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade));
        }
    }

    [Test]
    public void AnOwnerCarriesALabelAtMostOnce()
    {
        var assignment = Model.FindEntityType(typeof(LabelAssignment));

        Assert.That(assignment, Is.Not.Null);

        var unique = assignment.GetIndexes()
            .Where(static index => index.IsUnique)
            .Select(static index => index.Properties.Select(static property => property.Name).ToArray())
            .ToList();

        string[] onACase = [nameof(LabelAssignment.TenantId), nameof(LabelAssignment.LabelId), nameof(LabelAssignment.CaseId)];
        string[] onAnAct = [nameof(LabelAssignment.TenantId), nameof(LabelAssignment.LabelId), nameof(LabelAssignment.ActId)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unique, Does.Contain(onACase).Using<string[], string[]>(static (left, right) => left.SequenceEqual(right, StringComparer.Ordinal)));
            Assert.That(unique, Does.Contain(onAnAct).Using<string[], string[]>(static (left, right) => left.SequenceEqual(right, StringComparer.Ordinal)));
        }
    }
}

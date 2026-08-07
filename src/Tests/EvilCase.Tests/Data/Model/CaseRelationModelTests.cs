using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class CaseRelationModelTests
{
    [Test]
    public void ARelationIsOneRowPerOrderedPairOfDistinctCases()
    {
        // The read-optimized model drops check constraints; only the design-time one carries them.
        var designTime = ModelFixture.DesignTimeModel.FindEntityType(typeof(CaseRelation));
        var relation = ModelFixture.Model.FindEntityType(typeof(CaseRelation));

        Assert.That(new[] { designTime, relation }, Has.None.Null, "the relation is mapped");

        var check = designTime!.GetCheckConstraints().SingleOrDefault();
        var key = relation!.FindPrimaryKey();
        string[] pair = [nameof(CaseRelation.CaseId), nameof(CaseRelation.RelatedCaseId)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                check?.Sql?.Replace(" ", "", StringComparison.Ordinal),
                Is.AnyOf(@"""CaseId""<""RelatedCaseId""", @"""RelatedCaseId"">""CaseId"""),
                "the pair is stored in one order, which is also what refuses a case related to itself");
            Assert.That(key?.Properties.Select(property => property.Name), Is.EqualTo(pair), "the pair is the key, so one pair is one row whichever end asks");
            Assert.That(ModelFixture.IsIndexed(relation, nameof(CaseRelation.RelatedCaseId)), Is.True, "a relation is read from either end, so both columns are indexed");
            Assert.That(ModelFixture.ColumnsOf(relation), Is.EquivalentTo(pair), "the row is bare — it carries the pair and nothing else, not even an identity of its own");
            Assert.That(relation.GetNavigations(), Is.Empty, "the two ends are the same kind of end, so a read names both columns rather than following one of them");
        }
    }

    /// <summary>
    /// The two cascades are what makes the delete symmetric: the relation goes from whichever end is
    /// deleted, and neither of them reaches the case at the other end.
    /// </summary>
    [Test]
    public void DeletingACaseTakesItsRelationsAndLeavesTheCasesItRelatedTo()
    {
        var relation = ModelFixture.Model.FindEntityType(typeof(CaseRelation));

        Assert.That(relation, Is.Not.Null);

        var toCases = relation.GetForeignKeys().Where(key => key.PrincipalEntityType.ClrType == typeof(Case)).ToList();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toCases, Has.Count.EqualTo(2), "a relation names both ends");
            Assert.That(toCases.TrueForAll(key => key.DeleteBehavior == DeleteBehavior.Cascade), Is.True, "a relation has no meaning without either of its cases");
            Assert.That(
                ModelFixture.Model.GetEntityTypes().Any(entityType => entityType.ClrType == typeof(Case)
                    && entityType.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Case))),
                Is.False,
                "nothing cascades from one case to another, so a delete stops at the relation");
        }
    }
}

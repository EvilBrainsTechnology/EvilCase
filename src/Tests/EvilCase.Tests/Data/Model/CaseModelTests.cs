using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class CaseModelTests
{
    /// <summary>
    /// The vocabulary of <c>docs/product/vision.md</c>, held against the storage name rather than the
    /// CLR one — a <c>HasColumnName</c> back to the old name would otherwise go unnoticed.
    /// </summary>
    [Test]
    public void TheCaseNumberIsStoredUnderTheNameTheVisionGivesIt()
    {
        var @case = ModelFixture.Model.FindEntityType(typeof(Case));

        Assert.That(ModelFixture.ColumnsOf(@case), Has.Member("CaseNumber"), "the case's own mark is stored in a column named CaseNumber");
    }

    [Test]
    public void TheCasesOwnNumberIsAColumnOnTheCase()
    {
        var @case = ModelFixture.Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        var mark = @case.FindProperty(nameof(Case.CaseNumber));
        var unique = @case.GetIndexes().SingleOrDefault(index => index.IsUnique);
        string[] expected = [nameof(Case.OwnerId), nameof(Case.CaseNumber)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mark, Is.Not.Null, "a case always has exactly one of its own, so it is a column and not a row");
            Assert.That(mark?.IsNullable, Is.False, "it is generated with the case and never absent");
            Assert.That(unique?.Properties.Select(property => property.Name), Is.EqualTo(expected), "and a generated series must not repeat within one owner");
        }
    }

    [Test]
    public void ACaseHangsUnderNothing()
    {
        var @case = ModelFixture.Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.FindProperty("ParentCaseId"), Is.Null, "a case relates to another case, and neither of them is above the other");
            Assert.That(@case.GetForeignKeys().Any(key => key.PrincipalEntityType.ClrType == typeof(Case)), Is.False, "a self-reference is a hierarchy, whatever it is called");
        }
    }
}

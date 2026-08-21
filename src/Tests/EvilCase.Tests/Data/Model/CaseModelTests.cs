using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class CaseModelTests : ModelFixture
{
    /// <summary>
    /// The vocabulary of <c>docs/product/vision.md</c>, held against the storage names rather than the
    /// CLR ones — a <c>HasColumnName</c> back to the old name would otherwise go unnoticed.
    /// </summary>
    [Test]
    public void EveryIdentifierIsStoredUnderTheNameTheVisionGivesIt()
    {
        var @case = Model.FindEntityType(typeof(Case));

        Assert.That(ColumnsOf(@case), Has.Member("CaseNumber"), "the case's own mark is stored in a column named CaseNumber");
    }

    [Test]
    public void TheCasesOwnNumberIsAColumnOnTheCase()
    {
        var @case = Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        var mark = @case.FindProperty(nameof(Case.CaseNumber));
        var unique = @case.GetIndexes().SingleOrDefault(index => index.IsUnique);
        string[] expected = [nameof(Case.TenantId), nameof(Case.CaseNumber)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(mark, Is.Not.Null, "a case always has exactly one of its own, so it is a column and not a row");
            Assert.That(mark?.IsNullable, Is.False, "it is generated with the case and never absent");
            Assert.That(unique?.Properties.Select(property => property.Name), Is.EqualTo(expected), "and a generated series must not repeat within one tenant");
            Assert.That(unique?.GetDatabaseName(), Is.EqualTo(NumberConflict.CaseNumberIndex), "and the retry recognises the race by that index name");
        }
    }

    [Test]
    public void TheCaseDescriptionIsAsLongAsItNeedsToBe()
    {
        var @case = Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);
        Assert.That(@case.FindProperty(nameof(Case.Description))?.GetMaxLength(), Is.Null, "a case description is free text and carries no cap");
    }

    [Test]
    public void NothingIsIndexedByTheCaseDate()
    {
        var @case = Model.FindEntityType(typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case, Is.Not.Null);
            Assert.That(IsIndexed(@case!, nameof(Case.Date)), Is.False, "no read filters or orders cases by their date, so the index would only cost writes");
        }
    }

    [Test]
    public void ACaseHangsUnderAnOptionalParent()
    {
        var @case = Model.FindEntityType(typeof(Case));

        Assert.That(@case, Is.Not.Null);

        var selfFk = ForeignKeyTo<Case>(@case);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case.FindProperty(nameof(Case.ParentCaseId)), Is.Not.Null);
            Assert.That(@case.FindProperty(nameof(Case.ParentCaseId))?.IsNullable, Is.True);
            Assert.That(selfFk, Is.Not.Null);
            Assert.That(selfFk?.DeleteBehavior, Is.EqualTo(DeleteBehavior.SetNull), "a deleted parent orphans its children rather than taking them");
        }
    }
}

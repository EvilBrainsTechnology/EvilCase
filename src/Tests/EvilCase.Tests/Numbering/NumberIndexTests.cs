using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data.Model;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Numbering;

public class NumberIndexTests : ModelFixture
{
    [Test]
    public void TheUniqueIndexOfANumberIsNamedAfterItsColumn()
    {
        var @case = Model.FindEntityType(typeof(Case));
        var act = Model.FindEntityType(typeof(Act));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(@case, Is.Not.Null);
            Assert.That(act, Is.Not.Null);
        }

        var caseNumberIndexes = @case!.GetIndexes()
            .Where(index => index.IsUnique && index.Properties.Select(property => property.Name).Contains(nameof(Case.CaseNumber)))
            .ToList();
        var actNumberIndexes = act!.GetIndexes()
            .Where(index => index.IsUnique && index.Properties.Select(property => property.Name).Contains(nameof(Act.ActNumber)))
            .ToList();

        string[] expectedCaseIndex = ["TenantId", nameof(Case.CaseNumber)];
        string[] expectedActIndex = ["TenantId", nameof(Act.ActNumber)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseNumberIndexes, Has.Count.EqualTo(1));
            Assert.That(caseNumberIndexes[0].Properties.Select(property => property.Name), Is.EqualTo(expectedCaseIndex));
            Assert.That(
                caseNumberIndexes[0].GetDatabaseName(),
                Does.EndWith(nameof(Case.CaseNumber)),
                "the retry recognises the race by the index name");

            Assert.That(actNumberIndexes, Has.Count.EqualTo(1));
            Assert.That(actNumberIndexes[0].Properties.Select(property => property.Name), Is.EqualTo(expectedActIndex));
            Assert.That(
                actNumberIndexes[0].GetDatabaseName(),
                Does.EndWith(nameof(Act.ActNumber)),
                "the retry recognises the race by the index name");
        }
    }
}

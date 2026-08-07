using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ExternalCaseNumberModelTests
{
    /// <summary>
    /// The vocabulary of <c>docs/product/vision.md</c>, held against the storage name rather than the
    /// CLR one — a <c>HasColumnName</c> back to the old name would otherwise go unnoticed.
    /// </summary>
    [Test]
    public void AnAssignedMarkIsARowOfTheExternalCaseNumbersTable()
    {
        var external = ModelFixture.Model.FindEntityType(typeof(ExternalCaseNumber));

        Assert.That(external?.GetTableName(), Is.EqualTo("ExternalCaseNumbers"), "a mark somebody else assigned is a row of the ExternalCaseNumbers table");
    }

    [Test]
    public void EveryExternalMarkNamesWhoAssignedIt()
    {
        var external = ModelFixture.Model.FindEntityType(typeof(ExternalCaseNumber));

        Assert.That(external, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                external.FindProperty(nameof(ExternalCaseNumber.AssignedByPartyId))?.IsNullable,
                Is.False,
                "a mark nobody assigned is the case's own, and that one lives on the case");

            Assert.That(
                external.GetIndexes().Any(index => index.GetFilter() is not null),
                Is.False,
                "nothing here is conditional any more — this table is external marks and only those");
        }
    }

    [Test]
    public void AMarkNeverTakesAPartyDownWithIt()
    {
        var external = ModelFixture.Model.FindEntityType(typeof(ExternalCaseNumber));

        Assert.That(external, Is.Not.Null);

        var toParty = external.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Party));
        var toCase = external.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toParty?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict), "a party accumulates history across cases and outlives any one mark naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "a mark has no meaning without its case");
            Assert.That(external.GetProperties().Any(property => string.Equals(property.Name, "OwnerId", StringComparison.Ordinal)), Is.False, "only aggregate roots carry an owner, and a mark is not one");
        }
    }
}

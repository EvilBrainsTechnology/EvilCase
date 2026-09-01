using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ExternalNumberModelTests : ModelFixture
{
    [TestCase(typeof(ExternalCaseNumber), "ExternalCaseNumbers")]
    [TestCase(typeof(ExternalActNumber), "ExternalActNumbers")]
    public void EveryExternalNumberIsARowOfItsOwnTable(Type type, string table)
    {
        var external = Model.FindEntityType(type);

        Assert.That(external, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(external.GetTableName(), Is.EqualTo(table));
            Assert.That(
                Model.FindEntityType(typeof(Act))?.FindProperty("ExternalActNumber"),
                Is.Null,
                "the column was replaced by a table");
        }
    }

    [TestCase(typeof(ExternalCaseNumber))]
    [TestCase(typeof(ExternalActNumber))]
    public void EveryExternalNumberNamesWhoAssignedIt(Type type)
    {
        var external = Model.FindEntityType(type);

        Assert.That(external, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                external.FindProperty(nameof(ExternalCaseNumber.AssignedByContactId))?.IsNullable,
                Is.False,
                "a mark nobody assigned is the owner's own, and that one lives on the owner");

            Assert.That(
                external.GetIndexes().Any(static index => index.GetFilter() is not null),
                Is.False,
                "nothing here is conditional any more — this table is external marks and only those");
        }
    }

    [TestCase(typeof(ExternalCaseNumber), typeof(Case))]
    [TestCase(typeof(ExternalActNumber), typeof(Act))]
    public void AnExternalNumberNeverTakesAContactDownWithIt(Type type, Type ownerType)
    {
        var external = Model.FindEntityType(type);

        Assert.That(external, Is.Not.Null);

        var toContact = ForeignKeyTo<Contact>(external!);
        var toOwner = external!.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == ownerType);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toContact?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Restrict), "a contact accumulates history across cases and outlives any one mark naming it");
            Assert.That(toOwner?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "a mark has no meaning without its owner");
        }
    }

    [Test]
    public void ValuesAreUniquePerOwnerInsideTheTenant()
    {
        var caseNumber = Model.FindEntityType(typeof(ExternalCaseNumber));
        var actNumber = Model.FindEntityType(typeof(ExternalActNumber));

        Assert.That(new[] { caseNumber, actNumber }, Has.None.Null);

        var caseUnique = caseNumber!.GetIndexes().SingleOrDefault(static index => index.IsUnique);
        var actUnique = actNumber!.GetIndexes().SingleOrDefault(static index => index.IsUnique);
        string[] expectedCase = [nameof(ExternalCaseNumber.TenantId), nameof(ExternalCaseNumber.CaseId), nameof(ExternalCaseNumber.Value)];
        string[] expectedAct = [nameof(ExternalActNumber.TenantId), nameof(ExternalActNumber.ActId), nameof(ExternalActNumber.Value)];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseUnique?.Properties.Select(static property => property.Name), Is.EqualTo(expectedCase));
            Assert.That(actUnique?.Properties.Select(static property => property.Name), Is.EqualTo(expectedAct));
        }
    }
}

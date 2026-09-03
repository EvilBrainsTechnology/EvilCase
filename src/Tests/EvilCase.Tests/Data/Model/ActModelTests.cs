using System.Reflection;
using System.Runtime.CompilerServices;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ActModelTests : ModelFixture
{
    /// <summary>
    /// The vocabulary of <c>docs/product/vision.md</c>, held against the storage names rather than the
    /// CLR ones — a <c>HasColumnName</c> back to the old name would otherwise go unnoticed.
    /// </summary>
    [Test]
    public void EveryIdentifierIsStoredUnderTheNameTheVisionGivesIt()
    {
        var act = Model.FindEntityType(typeof(Act));
        var columns = ColumnsOf(act);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(columns, Has.Member("ActNumber"), "the act's own mark is stored in a column named ActNumber");
            Assert.That(columns, Has.Member("ExternalNumber"), "the number the issuing authority gave an act is a column on the act");
        }
    }

    [Test]
    public void AnActCarriesOneMandatoryDateAndNoOrderingNumber()
    {
        var act = Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var date = act.FindProperty(nameof(Act.Date));
        var others = act.GetProperties()
            .Where(static property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateOnly))
            .Where(static property => !string.Equals(property.Name, nameof(Act.Date), StringComparison.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(date?.ClrType, Is.EqualTo(typeof(DateOnly)), "the act date is a calendar date, and the hour never enters the period arithmetic it starts");
            Assert.That(typeof(Act).GetProperty(nameof(Act.Date))?.GetCustomAttribute<RequiredMemberAttribute>(), Is.Not.Null, "an act cannot be constructed without its date");
            Assert.That(others.Select(static property => property.Name), Is.Empty, "the act date is the only date an act carries");
            Assert.That(act.FindProperty("Ordinal"), Is.Null, "an act is ordered by its date alone, so it carries no ordering number");
        }
    }

    [Test]
    public void TheActDescriptionIsAsLongAsItNeedsToBe()
    {
        var act = Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);
        Assert.That(act.FindProperty(nameof(Act.Description))?.GetMaxLength(), Is.Null, "an act description is free text and carries no cap");
    }

    [Test]
    public void AnActIsIndexedByItsCaseAndNotByItsDate()
    {
        var act = Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var byCase = act.GetIndexes().SingleOrDefault(static index => index.Properties.Select(static property => property.Name).SequenceEqual([nameof(Act.CaseId)], StringComparer.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byCase, Is.Not.Null, "deleting a case reads its acts by CaseId");
            Assert.That(IsIndexed(act, nameof(Act.Date)), Is.False, "no read filters or orders acts by their date yet");
        }
    }

    [Test]
    public void AnActNeverTakesAContactDownWithIt()
    {
        var act = Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var toContacts = act.GetForeignKeys().Where(static key => key.PrincipalEntityType.ClrType == typeof(Contact)).ToList();
        var toCase = act.GetForeignKeys().SingleOrDefault(static key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toContacts, Has.Count.EqualTo(2), "an act references its sender and its recipient");
            Assert.That(toContacts.TrueForAll(static key => key.DeleteBehavior == DeleteBehavior.Restrict), Is.True, "a contact outlives any one act naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "an act has no meaning without its case");
        }
    }

    [Test]
    public void TheActsOwnNumberIsUniqueWithinTheTenant()
    {
        var act = Model.FindEntityType(typeof(Act));
        var unique = act?.GetIndexes().SingleOrDefault(static index => index.IsUnique);
        string[] expected = [nameof(Act.TenantId), nameof(Act.ActNumber)];

        Assert.That(unique?.Properties.Select(static property => property.Name), Is.EqualTo(expected), "a generated series must not repeat within one tenant");
    }

    [Test]
    public void TheExternalNumberIsOneOptionalColumnOnTheAct()
    {
        var act = Model.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var number = act.FindProperty(nameof(Act.ExternalNumber));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(number, Is.Not.Null, "an act carries at most one number another authority gave it, so it is a column and not a row");
            Assert.That(number?.IsNullable, Is.True, "an act exists before anybody records one");
            Assert.That(number?.GetMaxLength(), Is.EqualTo(128));
            Assert.That(IsIndexed(act!, nameof(Act.ExternalNumber)), Is.False, "no read filters or orders acts by it");
        }
    }
}

using System.Reflection;
using System.Runtime.CompilerServices;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;
using static EvilBrains.EvilCase.Tests.Data.Model.ModelFixture;

namespace EvilBrains.EvilCase.Tests.Data.Model;

public class ActModelTests
{
    /// <summary>
    /// The vocabulary of <c>docs/product/vision.md</c>, held against the storage names rather than the
    /// CLR ones — a <c>HasColumnName</c> back to the old name would otherwise go unnoticed.
    /// </summary>
    [Test]
    public void TheExternalActNumberIsStoredUnderTheNameTheVisionGivesIt()
    {
        var act = Runtime.FindEntityType(typeof(Act));

        Assert.That(ColumnsOf(act), Has.Member("ExternalActNumber"), "the number the issuing authority gave an act is stored in a column named ExternalActNumber");
    }

    [Test]
    public void AnActCarriesOneMandatoryDateAndNoOrderingNumber()
    {
        var act = Runtime.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var date = act.FindProperty(nameof(Act.Date));
        var others = act.GetProperties()
            .Where(property => (Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) == typeof(DateOnly)
                && !string.Equals(property.Name, nameof(Act.Date), StringComparison.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(date?.ClrType, Is.EqualTo(typeof(DateOnly)), "the act date is a calendar date, and the hour never enters the period arithmetic it starts");
            Assert.That(typeof(Act).GetProperty(nameof(Act.Date))?.GetCustomAttribute<RequiredMemberAttribute>(), Is.Not.Null, "an act cannot be constructed without its date");
            Assert.That(others.Select(property => property.Name), Is.Empty, "the act date is the only date an act carries");
            Assert.That(act.FindProperty("Ordinal"), Is.Null, "an act is ordered by its date alone, so it carries no ordering number");
        }
    }

    [Test]
    public void TheActSummaryIsAsLongAsItNeedsToBe()
    {
        var act = Runtime.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        Assert.That(act.FindProperty(nameof(Act.Summary))?.GetMaxLength(), Is.Null, "the summary is long-form and lives on the act alone");
    }

    [Test]
    public void ActsAreIndexedForOrderingByDateWithinACase()
    {
        var act = Runtime.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        string[] expected = [nameof(Act.CaseId), nameof(Act.Date)];
        var byDate = act.GetIndexes().SingleOrDefault(index => index.Properties.Select(property => property.Name).SequenceEqual(expected, StringComparer.Ordinal));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byDate, Is.Not.Null, "an act list reads one case ordered by the act date, and (CaseId, Date) is what serves it");
            Assert.That(byDate?.IsUnique, Is.False, "two acts of one case share a date whenever they were filed on the same day");
        }
    }

    [Test]
    public void AnActNeverTakesAPartyDownWithIt()
    {
        var act = Runtime.FindEntityType(typeof(Act));

        Assert.That(act, Is.Not.Null);

        var toParties = act.GetForeignKeys().Where(key => key.PrincipalEntityType.ClrType == typeof(Party)).ToList();
        var toCase = act.GetForeignKeys().SingleOrDefault(key => key.PrincipalEntityType.ClrType == typeof(Case));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(toParties, Has.Count.EqualTo(2), "an act references who issued it and who it was addressed to");
            Assert.That(toParties.TrueForAll(key => key.DeleteBehavior == DeleteBehavior.Restrict), Is.True, "a party outlives any one act naming it");
            Assert.That(toCase?.DeleteBehavior, Is.EqualTo(DeleteBehavior.Cascade), "an act has no meaning without its case");
        }
    }
}

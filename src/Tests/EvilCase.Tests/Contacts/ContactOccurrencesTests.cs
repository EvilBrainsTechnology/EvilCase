using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Pure merge and order of the two act occurrence sources — no database.
/// </summary>
public class ContactOccurrencesTests
{
    [Test]
    public void TheNewestActComesFirst()
    {
        var oldest = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001");
        var middle = Occurrence(date: new DateOnly(2024, 6, 1), number: "EC/20240101-001/20240601-001");
        var newest = Occurrence(date: new DateOnly(2024, 12, 1), number: "EC/20240101-001/20241201-001");

        var order = ContactOccurrences.InDisplayOrder([oldest, middle, newest], []);

        Guid[] expected = [newest.ActId, middle.ActId, oldest.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected));
    }

    [Test]
    public void ActsOfTheSameDayAreOrderedByTheirNumberFromTheBack()
    {
        var lower = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001");
        var higher = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-002");

        var order = ContactOccurrences.InDisplayOrder([lower, higher], []);

        Guid[] expected = [higher.ActId, lower.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "the act number runs from the back, same as the case number");
    }

    [Test]
    public void AnActNumberPastNineHundredNinetyNineComesFirst()
    {
        var low = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-002");
        var high = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-999");
        var pastAThousand = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-1000");

        var order = ContactOccurrences.InDisplayOrder([low, high, pastAThousand], []);

        Guid[] expected = [pastAThousand.ActId, high.ActId, low.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "the number's length decides before its text, so a sequence that grew a digit follows the one below it");
    }

    [Test]
    public void OneActNamingTheContactTwiceYieldsTwoRows()
    {
        var actId = Guid.CreateVersion7();
        var issuedBy = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001", actId: actId, role: ContactActRole.IssuedBy);
        var addressedTo = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001", actId: actId, role: ContactActRole.AddressedTo);

        var order = ContactOccurrences.InDisplayOrder([issuedBy], [addressedTo]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(order, Has.Count.EqualTo(2), "an act that names the contact in two roles stands for one row per role");

            ContactActRole[] expectedRoles = [ContactActRole.IssuedBy, ContactActRole.AddressedTo];

            Assert.That(order.Select(occurrence => occurrence.Role), Is.EquivalentTo(expectedRoles));
        }
    }

    private static ContactActOccurrence Occurrence(
        in DateOnly date,
        string number,
        Guid? actId = null,
        ContactActRole role = ContactActRole.IssuedBy)
    {
        return new()
        {
            ActId = actId ?? Guid.CreateVersion7(),
            ActNumber = number,
            ActTitle = "Úkon",
            ActDate = date,
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/20240101-001",
            Role = role,
        };
    }
}

using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Pure merge and order of the three act occurrence sources — no database.
/// </summary>
public class ContactOccurrencesTests
{
    [Test]
    public void TheNewestActComesFirst()
    {
        var oldest = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001");
        var middle = Occurrence(date: new DateOnly(2024, 6, 1), number: "EC/20240101-001/20240601-001");
        var newest = Occurrence(date: new DateOnly(2024, 12, 1), number: "EC/20240101-001/20241201-001");

        var order = ContactOccurrences.InDisplayOrder([oldest, middle, newest], [], []);

        Guid[] expected = [newest.ActId, middle.ActId, oldest.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected));
    }

    [Test]
    public void ActsOfTheSameDayAreOrderedByTheirNumberFromTheBack()
    {
        var lower = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001");
        var higher = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-002");

        var order = ContactOccurrences.InDisplayOrder([lower, higher], [], []);

        Guid[] expected = [higher.ActId, lower.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "the act number runs from the back, same as the case number");
    }

    [Test]
    public void AnActNumberPastNineHundredNinetyNineComesFirst()
    {
        var low = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-002");
        var high = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-999");
        var pastAThousand = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-1000");

        var order = ContactOccurrences.InDisplayOrder([low, high, pastAThousand], [], []);

        Guid[] expected = [pastAThousand.ActId, high.ActId, low.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "a sequence follows the one below it however many digits it grew");
    }

    [Test]
    public void TheCaseNumberDecidesBeforeTheActNumber()
    {
        var newerCase = Occurrence(date: new DateOnly(2026, 9, 1), number: "EC/20260812-001/20260901-001", caseNumber: "EC/20260812-001");
        var olderCase = Occurrence(date: new DateOnly(2026, 9, 1), number: "EC/20260807-1000/20260901-001", caseNumber: "EC/20260807-1000");

        var order = ContactOccurrences.InDisplayOrder([olderCase, newerCase], [], []);

        Guid[] expected = [newerCase.ActId, olderCase.ActId];

        Assert.That(
            order.Select(occurrence => occurrence.ActId),
            Is.EqualTo(expected),
            "the case an act sits in decides first, whatever the length of the two numbers");
    }

    [Test]
    public void ANumberWrittenByHandSortsBehindTheIssuedOnes()
    {
        var written = Occurrence(date: new DateOnly(2026, 8, 12), number: "1 T 5/2026-14", caseNumber: "1 T 5/2026");
        var issued = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/20260807-001/20260812-001");

        var order = ContactOccurrences.InDisplayOrder([written, issued], [], []);

        Guid[] expected = [issued.ActId, written.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "a number outside the format names no day and no sequence");
    }

    [Test]
    public void OneActNamingTheContactTwiceYieldsTwoRowsIssuedByFirst()
    {
        var actId = Guid.CreateVersion7();
        var issuedBy = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001", actId: actId, role: ContactActRole.IssuedBy);
        var numberIssuer = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/20240101-001/20240101-001", actId: actId, role: ContactActRole.NumberIssuer);

        var order = ContactOccurrences.InDisplayOrder([issuedBy], [], [numberIssuer]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(order, Has.Count.EqualTo(2));

            ContactActRole[] expectedRoles = [ContactActRole.IssuedBy, ContactActRole.NumberIssuer];

            Assert.That(order.Select(occurrence => occurrence.Role), Is.EqualTo(expectedRoles));
        }
    }

    private static ContactActOccurrence Occurrence(
        in DateOnly date,
        string number,
        string caseNumber = "EC/20240101-001",
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
            CaseNumber = caseNumber,
            Role = role,
        };
    }
}

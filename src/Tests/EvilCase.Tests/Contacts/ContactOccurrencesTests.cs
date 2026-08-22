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
        var oldest = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024/001");
        var middle = Occurrence(date: new DateOnly(2024, 6, 1), number: "EC/2024/002");
        var newest = Occurrence(date: new DateOnly(2024, 12, 1), number: "EC/2024/003");

        var order = ContactOccurrences.InDisplayOrder([oldest, middle, newest], [], []);

        Guid[] expected = [newest.ActId, middle.ActId, oldest.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected));
    }

    [Test]
    public void ActsOfTheSameDayAreOrderedByTheirNumberFromTheBack()
    {
        var lower = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024-001");
        var higher = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024-002");

        var order = ContactOccurrences.InDisplayOrder([lower, higher], [], []);

        Guid[] expected = [higher.ActId, lower.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "the act number runs from the back, same as the case number");
    }

    [Test]
    public void TheActNumbersRunFromTheBackAndALongerNumberComesFirst()
    {
        var low = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/1/20260812-002");
        var high = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/1/20260812-999");
        var pastAThousand = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/1/20260812-1000");

        var order = ContactOccurrences.InDisplayOrder([low, high, pastAThousand], [], []);

        Guid[] expected = [pastAThousand.ActId, high.ActId, low.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "an act number past 999 is longer, and length decides before the text");
    }

    [Test]
    public void TheCaseNumbersRunFromTheBackBeforeTheActNumbers()
    {
        var higherCaseSecondAct = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/10/20260812-002");
        var higherCaseFirstAct = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/10/20260812-001");
        var lowerCaseSecondAct = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/9/20260812-002");
        var lowerCaseFirstAct = Occurrence(date: new DateOnly(2026, 8, 12), number: "EC/2024/9/20260812-001");

        var order = ContactOccurrences.InDisplayOrder(
            [lowerCaseFirstAct, higherCaseFirstAct, lowerCaseSecondAct, higherCaseSecondAct],
            [],
            []);

        Guid[] expected = [higherCaseSecondAct.ActId, higherCaseFirstAct.ActId, lowerCaseSecondAct.ActId, lowerCaseFirstAct.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected), "the case number prefixes the act number, so it runs from the back too");
    }

    [Test]
    public void OneActNamingTheContactTwiceYieldsTwoRowsIssuedByFirst()
    {
        var actId = Guid.CreateVersion7();
        var issuedBy = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024-001", actId: actId, role: ContactActRole.IssuedBy);
        var numberIssuer = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024-001", actId: actId, role: ContactActRole.NumberIssuer);

        var order = ContactOccurrences.InDisplayOrder([issuedBy], [], [numberIssuer]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(order, Has.Count.EqualTo(2));

            ContactActRole[] expectedRoles = [ContactActRole.IssuedBy, ContactActRole.NumberIssuer];

            Assert.That(order.Select(occurrence => occurrence.Role), Is.EqualTo(expectedRoles));
        }
    }

    private static ContactActOccurrence Occurrence(in DateOnly date, string number, Guid? actId = null, ContactActRole role = ContactActRole.IssuedBy)
    {
        return new()
        {
            ActId = actId ?? Guid.CreateVersion7(),
            ActNumber = number,
            ActTitle = "Úkon",
            ActDate = date,
            CaseId = Guid.CreateVersion7(),
            CaseNumber = "EC/2024",
            Role = role,
        };
    }
}

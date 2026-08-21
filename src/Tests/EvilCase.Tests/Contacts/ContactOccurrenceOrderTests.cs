using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// Pure merge and order of the three act occurrence sources — no database.
/// </summary>
public class ContactOccurrenceOrderTests
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
    public void ActsOfTheSameDayAreOrderedByTheirNumber()
    {
        var second = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024-002");
        var first = Occurrence(date: new DateOnly(2024, 1, 1), number: "EC/2024-001");

        var order = ContactOccurrences.InDisplayOrder([second, first], [], []);

        Guid[] expected = [first.ActId, second.ActId];

        Assert.That(order.Select(occurrence => occurrence.ActId), Is.EqualTo(expected));
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

    private static ContactActOccurrence Occurrence(in DateOnly date, string number, Guid? actId = null, ContactActRole role = ContactActRole.IssuedBy) => new()
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

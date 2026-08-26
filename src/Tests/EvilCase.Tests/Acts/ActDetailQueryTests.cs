using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// The one act's header, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class ActDetailQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private TestTenant tenant = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task TheDetailCarriesTheNumberTheDirectionTheDateTheTitleAndTheDescription()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day, "Rozhodnutí", direction: ActDirection.Outgoing, description: "Popis úkonu");

        var detail = await this.tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.ActId, Is.EqualTo(act.Id));
            Assert.That(detail.ActNumber, Is.EqualTo(act.ActNumber));
            Assert.That(detail.Direction, Is.EqualTo(ActDirection.Outgoing));
            Assert.That(detail.Date, Is.EqualTo(Day));
            Assert.That(detail.Title, Is.EqualTo("Rozhodnutí"));
            Assert.That(detail.Description, Is.EqualTo("Popis úkonu"));
        }
    }

    [Test]
    public async Task TheDetailNamesTheCaseTheActSitsIn()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);

        var detail = await this.tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.CaseId, Is.EqualTo(@case.Id), "the detail carries the case id the screens build their links from");
            Assert.That(detail.CaseNumber, Is.EqualTo(@case.CaseNumber), "the detail carries the case number the link back to the case reads");
        }
    }

    [Test]
    public async Task TheDetailCarriesBothContacts()
    {
        var @case = await this.tenant.AddCase(Day);
        var sender = await this.tenant.AddContact("Odesílatel", kind: ContactKind.Authority);
        var recipient = await this.tenant.AddContact("Příjemce", kind: ContactKind.Person);
        var act = await this.tenant.AddAct(@case, Day, issuedBy: sender, addressedTo: recipient);

        var detail = await this.tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.IssuedByContact.ContactId, Is.EqualTo(sender.Id));
            Assert.That(detail.IssuedByContact.Name, Is.EqualTo(sender.Name));
            Assert.That(detail.IssuedByContact.Kind, Is.EqualTo(sender.Kind));
            Assert.That(detail.AddressedToContact!.ContactId, Is.EqualTo(recipient.Id));
            Assert.That(detail.AddressedToContact.Name, Is.EqualTo(recipient.Name));
            Assert.That(detail.AddressedToContact.Kind, Is.EqualTo(recipient.Kind));
        }
    }

    [Test]
    public async Task AnActWithoutARecipientNamesNone()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);

        var detail = await this.tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.AddressedToContact, Is.Null, "a recipient is optional and its absence is no recipient, not a failed read");
    }

    [Test]
    public async Task AnActReadUnderAnotherCaseIsNoDetail()
    {
        var first = await this.tenant.AddCase(Day, "První");
        var second = await this.tenant.AddCase(Day, "Druhý");
        var act = await this.tenant.AddAct(first, Day);

        var detail = await this.tenant.Context.Acts.DetailOf(second.Id, act.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "an act is only ever read under the case it sits in");
    }

    [Test]
    public async Task AnUnknownIdIsNoDetail()
    {
        var @case = await this.tenant.AddCase(Day);

        var detail = await this.tenant.Context.Acts.DetailOf(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task AnActOfAnotherTenantIsNoDetail()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);

        var detail = await this.tenant.Context.Acts.DetailOf(otherCase.Id, otherAct.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "the tenant query filter is what turns another tenant's id into nothing");
    }

    [Test]
    public async Task TheReaderReturnsTheDetail()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));
        var detail = await reader.GetActDetail(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ActId, Is.EqualTo(act.Id));
    }

    [Test]
    public async Task TheDetailCarriesTheActsNumbersWithTheContactThatAssignedThem()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        var number = await this.tenant.AddExternalActNumber(act, "1 T 45/2026", contact);

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));
        var detail = await reader.GetActDetail(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.ExternalNumbers, Has.Count.EqualTo(1));
            Assert.That(detail.ExternalNumbers[0].ExternalNumberId, Is.EqualTo(number.Id));
            Assert.That(detail.ExternalNumbers[0].Value, Is.EqualTo("1 T 45/2026"));
            Assert.That(detail.ExternalNumbers[0].AssignedByContactId, Is.EqualTo(contact.Id));
            Assert.That(detail.ExternalNumbers[0].AssignedByContactName, Is.EqualTo(contact.Name));
        }
    }

    [Test]
    public async Task TheNumbersComeInTheOrderTheyAccrued()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        await this.tenant.AddExternalActNumber(act, "1 T 45/2026", contact);
        await this.tenant.AddExternalActNumber(act, "2 T 46/2026", contact);

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));
        var detail = await reader.GetActDetail(@case.Id, act.Id, CancellationToken.None);

        Assert.That(
            detail!.ExternalNumbers.Select(number => number.Value),
            Is.EqualTo(["1 T 45/2026", "2 T 46/2026"]),
            "a number's place in the list is the order it accrued");
    }

    [Test]
    public async Task AnotherActsNumbersAreNotListed()
    {
        var @case = await this.tenant.AddCase(Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        var actA = await this.tenant.AddAct(@case, Day, "A");
        var actB = await this.tenant.AddAct(@case, Day, "B");
        await this.tenant.AddExternalActNumber(actA, "1 T 45/2026", contact);
        await this.tenant.AddExternalActNumber(actB, "2 T 46/2026", contact);

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));
        var detail = await reader.GetActDetail(@case.Id, actA.Id, CancellationToken.None);

        Assert.That(detail!.ExternalNumbers.Select(number => number.Value), Is.EqualTo(["1 T 45/2026"]));
    }

    [Test]
    public async Task AnActWithNoNumbersCarriesAnEmptyList()
    {
        var @case = await this.tenant.AddCase(Day);
        var act = await this.tenant.AddAct(@case, Day);

        var reader = new ActReader(new FixedDbSession(this.tenant.Context));
        var detail = await reader.GetActDetail(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ExternalNumbers, Is.Empty);
    }
}

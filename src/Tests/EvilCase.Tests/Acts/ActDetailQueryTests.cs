using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// The one act's header, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class ActDetailQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheDetailCarriesTheNumberTheDirectionTheDateTheTitleAndTheDescription()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, "Rozhodnutí", direction: ActDirection.Outgoing, description: "Popis úkonu");

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

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
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.CaseId, Is.EqualTo(@case.Id), "the detail carries the case id the screens build their links from");
            Assert.That(detail.CaseNumber, Is.EqualTo(@case.CaseNumber), "the detail carries the case number the link back to the case reads");
        }
    }

    [Test]
    public async Task TheDetailCarriesBothContacts()
    {
        var @case = await this.Tenant.AddCase(Day);
        var sender = await this.Tenant.AddContact("Odesílatel", kind: ContactKind.Authority);
        var recipient = await this.Tenant.AddContact("Příjemce", kind: ContactKind.Person);
        var act = await this.Tenant.AddAct(@case, Day, issuedBy: sender, addressedTo: recipient);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

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
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.AddressedToContact, Is.Null, "a recipient is optional and its absence is no recipient, not a failed read");
    }

    [Test]
    public async Task AnActReadUnderAnotherCaseIsNoDetail()
    {
        var first = await this.Tenant.AddCase(Day, "První");
        var second = await this.Tenant.AddCase(Day, "Druhý");
        var act = await this.Tenant.AddAct(first, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(second.Id, act.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "an act is only ever read under the case it sits in");
    }

    [Test]
    public async Task AnUnknownIdIsNoDetail()
    {
        var @case = await this.Tenant.AddCase(Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task AnActOfAnotherTenantIsNoDetail()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(otherCase.Id, otherAct.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "the tenant query filter is what turns another tenant's id into nothing");
    }

    [Test]
    public async Task TheReaderReturnsTheDetail()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));
        var detail = await reader.GetActDetail(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ActId, Is.EqualTo(act.Id));
    }

    [Test]
    public async Task TheDetailCarriesTheActsExternalReferenceNumber()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day, externalNumber: "1 T 45/2026");

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ExternalNumber, Is.EqualTo("1 T 45/2026"));
    }

    [Test]
    public async Task AnActWithNoExternalReferenceNumberNamesNone()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ExternalNumber, Is.Null, "the number is optional and its absence is no number, not a failed read");
    }
}

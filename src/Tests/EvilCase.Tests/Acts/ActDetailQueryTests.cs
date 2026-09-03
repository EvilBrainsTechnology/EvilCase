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
        var act = await this.Tenant.AddAct(
            @case,
            Day,
            "Rozhodnutí",
            contact: await this.Tenant.AddContact("Městský úřad"),
            direction: ActDirection.Outgoing,
            description: "Popis úkonu");

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
    public async Task TheDetailCarriesTheActsContactAndTheCasesContact()
    {
        var caseContact = await this.Tenant.AddContact("Městský úřad Vzorov", kind: ContactKind.Authority);
        var actContact = await this.Tenant.AddContact("Krajský soud ve Vzorově", kind: ContactKind.Person);
        var @case = await this.Tenant.AddCase(Day, contact: caseContact);
        var act = await this.Tenant.AddAct(@case, Day, contact: actContact);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.Contact!.ContactId, Is.EqualTo(actContact.Id));
            Assert.That(detail.Contact.Name, Is.EqualTo(actContact.Name));
            Assert.That(detail.Contact.Kind, Is.EqualTo(actContact.Kind));
            Assert.That(detail.CaseContact!.ContactId, Is.EqualTo(caseContact.Id));
        }
    }

    [Test]
    public async Task AnActWithNoContactNamesNone()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.Contact, Is.Null, "a contact is optional and its absence is no contact, not a failed read");
            Assert.That(detail.CaseContact, Is.Null);
        }
    }

    [Test]
    public async Task TheDetailNamesTheCasesContactEvenWhereTheActNamesNone()
    {
        var caseContact = await this.Tenant.AddContact("Městský úřad Vzorov");
        var @case = await this.Tenant.AddCase(Day, contact: caseContact);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.Contact, Is.Null);
            Assert.That(detail.CaseContact!.ContactId, Is.EqualTo(caseContact.Id), "the warning on the act is built from the case's contact");
        }
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
        var act = await this.Tenant.AddAct(@case, Day, externalActNumber: "1 T 45/2026");

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ExternalActNumber, Is.EqualTo("1 T 45/2026"));
    }

    [Test]
    public async Task AnActWithNoExternalReferenceNumberNamesNone()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var detail = await this.Tenant.Context.Acts.DetailOf(@case.Id, act.Id, CancellationToken.None);

        Assert.That(detail!.ExternalActNumber, Is.Null, "the number is optional and its absence is no number, not a failed read");
    }
}

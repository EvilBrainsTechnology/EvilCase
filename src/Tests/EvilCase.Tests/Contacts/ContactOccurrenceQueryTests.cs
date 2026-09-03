using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The two places a contact is named, on the rows a real PostgreSQL returns. Each test seeds a tenant
/// of its own, so none cleans up after itself.
/// </summary>
public class ContactOccurrenceQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    [Test]
    public async Task TheIssuerAndTheAddresseeEachNarrowByTheirOwnColumn()
    {
        var authority = await this.Tenant.AddContact("Městský úřad");
        var person = await this.Tenant.AddContact("Jan Novák", ContactKind.Person);
        var @case = await this.Tenant.AddCase(Day);
        var issued = await this.Tenant.AddAct(@case, Day, "Rozhodnutí", issuedBy: authority, addressedTo: person, externalNumber: "MUVZ/2026/117");

        var issuedBy = await this.Tenant.Context.Acts
            .IssuedByContact(authority.Id)
            .AsActOccurrences(ContactActRole.IssuedBy)
            .ToListAsync();

        var addressedTo = await this.Tenant.Context.Acts
            .AddressedToContact(authority.Id)
            .AsActOccurrences(ContactActRole.AddressedTo)
            .ToListAsync();

        var expected = new ContactActOccurrence
        {
            ActId = issued.Id,
            ActNumber = issued.ActNumber,
            ActTitle = "Rozhodnutí",
            ActDate = Day,
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            Role = ContactActRole.IssuedBy,
            ExternalNumber = "MUVZ/2026/117",
        };

        ContactActOccurrence[] expectedRows = [expected];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(issuedBy, Is.EqualTo(expectedRows), "an act occurrence carries the number another authority gave the act");
            Assert.That(addressedTo, Is.Empty, "the addressee source never answers for the issuer column");
        }
    }

    [Test]
    public async Task TheAddresseeSourceCarriesItsOwnRole()
    {
        var authority = await this.Tenant.AddContact("Městský úřad");
        var person = await this.Tenant.AddContact("Jan Novák", ContactKind.Person);
        var @case = await this.Tenant.AddCase(Day);
        var addressed = await this.Tenant.AddAct(@case, Day, "Výzva", issuedBy: authority, addressedTo: person);

        var occurrence = await this.Tenant.Context.Acts
            .AddressedToContact(person.Id)
            .AsActOccurrences(ContactActRole.AddressedTo)
            .SingleAsync();

        var expected = new ContactActOccurrence
        {
            ActId = addressed.Id,
            ActNumber = addressed.ActNumber,
            ActTitle = "Výzva",
            ActDate = Day,
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            Role = ContactActRole.AddressedTo,
            ExternalNumber = null,
        };

        Assert.That(occurrence, Is.EqualTo(expected), "the addressee source names the role it stands for");
    }

    [Test]
    public async Task AnOccurrenceOfAnotherTenantNeverComesBack()
    {
        Guid contactId;

        await using (var other = await TestTenant.Create())
        {
            var otherContact = await other.AddContact("Cizí úřad");
            var otherCase = await other.AddCase(Day);
            await other.AddAct(otherCase, Day, issuedBy: otherContact);

            contactId = otherContact.Id;
        }

        var issuedBy = await this.Tenant.Context.Acts.IssuedByContact(contactId).CountAsync();
        var defaultContacts = await this.Tenant.Context.Users.WithDefaultContact(contactId).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(issuedBy, Is.Zero, "a query filter is what keeps another tenant's rows out");
            Assert.That(defaultContacts, Is.Zero, "the user's tenant query filter is what keeps another tenant's rows out");
        }
    }

    [Test]
    public async Task TheDefaultContactCheckFindsTheUserThatHoldsIt()
    {
        var other = await this.Tenant.AddContact("Městský úřad");

        var holdsDefault = await this.Tenant.Context.Users.WithDefaultContact(this.Tenant.DefaultContact.Id).CountAsync();
        var holdsOther = await this.Tenant.Context.Users.WithDefaultContact(other.Id).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(holdsDefault, Is.EqualTo(1), "the check finds the user whose default the contact is");
            Assert.That(holdsOther, Is.Zero, "a contact no user prefills with is nobody's default");
        }
    }
}

using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The four places a contact is named, on the rows a real PostgreSQL returns. Each test seeds a tenant
/// of its own, so none cleans up after itself.
/// </summary>
public class ContactOccurrenceQueryTests
{
    private static readonly DateOnly Day = new(2026, 8, 7);

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
    public async Task ACaseOccurrenceReachesTheCaseThroughTheExternalNumber()
    {
        var authority = await this.tenant.AddContact("Městský úřad");
        var @case = await this.tenant.AddCase(Day, "Přestupek");
        await this.tenant.AddExternalCaseNumber(@case, "MUB/2026/117", authority);

        var occurrence = await this.tenant.Context.ExternalCaseNumbers
            .AssignedByContact(authority.Id)
            .AsCaseOccurrences()
            .SingleAsync();

        var expected = new ContactCaseOccurrence
        {
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            CaseTitle = "Přestupek",
            CaseDate = Day,
            ExternalNumber = "MUB/2026/117",
        };

        Assert.That(occurrence, Is.EqualTo(expected), "an occurrence carries the case the external number hangs under");
    }

    [Test]
    public async Task AMarkAnotherContactAssignedNeverComesBack()
    {
        var authority = await this.tenant.AddContact("Městský úřad");
        var court = await this.tenant.AddContact("Krajský soud");
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddExternalCaseNumber(@case, "MUB/2026/117", authority);
        await this.tenant.AddExternalCaseNumber(@case, "KS/2026/42", court);

        var numbers = await this.tenant.Context.ExternalCaseNumbers
            .AssignedByContact(authority.Id)
            .AsCaseOccurrences()
            .Select(occurrence => occurrence.ExternalNumber)
            .ToListAsync();

        string[] expected = ["MUB/2026/117"];

        Assert.That(numbers, Is.EqualTo(expected), "the source narrows by the contact that assigned the mark");
    }

    [Test]
    public async Task TheCaseOccurrenceOrderPutsANumberThatGrewADigitFirst()
    {
        var authority = await this.tenant.AddContact("Městský úřad");
        var newer = await this.tenant.AddCase(new DateOnly(2026, 8, 20));
        var grown = await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 1000));
        var shorter = await this.tenant.AddCase(Day, caseNumber: CaseNumberFormat.Compose(Day, 999));

        await this.tenant.AddExternalCaseNumber(newer, "A", authority);
        await this.tenant.AddExternalCaseNumber(grown, "B", authority);
        await this.tenant.AddExternalCaseNumber(shorter, "C", authority);

        var caseIds = await this.tenant.Context.ExternalCaseNumbers
            .AssignedByContact(authority.Id)
            .InCaseOccurrenceOrder()
            .AsCaseOccurrences()
            .Select(occurrence => occurrence.CaseId)
            .ToListAsync();

        Guid[] expected = [newer.Id, grown.Id, shorter.Id];

        Assert.That(caseIds, Is.EqualTo(expected), "the case date orders first, and a sequence that grew a digit outranks a three-digit one");
    }

    [Test]
    public async Task TwoMarksOnOneCaseAreOrderedByTheirValue()
    {
        var authority = await this.tenant.AddContact("Městský úřad");
        var @case = await this.tenant.AddCase(Day);
        await this.tenant.AddExternalCaseNumber(@case, "MUB/2026/200", authority);
        await this.tenant.AddExternalCaseNumber(@case, "MUB/2026/117", authority);

        var numbers = await this.tenant.Context.ExternalCaseNumbers
            .AssignedByContact(authority.Id)
            .InCaseOccurrenceOrder()
            .AsCaseOccurrences()
            .Select(occurrence => occurrence.ExternalNumber)
            .ToListAsync();

        string[] expected = ["MUB/2026/117", "MUB/2026/200"];

        Assert.That(numbers, Is.EqualTo(expected), "two marks on one case are ordered by the mark itself");
    }

    [Test]
    public async Task TheIssuerAndTheAddresseeEachNarrowByTheirOwnColumn()
    {
        var authority = await this.tenant.AddContact("Městský úřad");
        var person = await this.tenant.AddContact("Jan Novák", ContactKind.Person);
        var @case = await this.tenant.AddCase(Day);
        var issued = await this.tenant.AddAct(@case, Day, "Rozhodnutí", issuedBy: authority, addressedTo: person);

        var issuedBy = await this.tenant.Context.Acts
            .IssuedByContact(authority.Id)
            .AsActOccurrences(ContactActRole.IssuedBy)
            .ToListAsync();

        var addressedTo = await this.tenant.Context.Acts
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
            ExternalNumber = null,
        };

        ContactActOccurrence[] expectedRows = [expected];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(issuedBy, Is.EqualTo(expectedRows), "the issuer source reads the acts the contact issued");
            Assert.That(addressedTo, Is.Empty, "the addressee source never answers for the issuer column");
        }
    }

    [Test]
    public async Task TheAddresseeSourceCarriesItsOwnRole()
    {
        var authority = await this.tenant.AddContact("Městský úřad");
        var person = await this.tenant.AddContact("Jan Novák", ContactKind.Person);
        var @case = await this.tenant.AddCase(Day);
        var addressed = await this.tenant.AddAct(@case, Day, "Výzva", issuedBy: authority, addressedTo: person);

        var occurrence = await this.tenant.Context.Acts
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
            await other.AddExternalCaseNumber(otherCase, "CIZ/2026/1", otherContact);
            await other.AddAct(otherCase, Day, issuedBy: otherContact);

            contactId = otherContact.Id;
        }

        var caseOccurrences = await this.tenant.Context.ExternalCaseNumbers.AssignedByContact(contactId).CountAsync();
        var issuedBy = await this.tenant.Context.Acts.IssuedByContact(contactId).CountAsync();
        var defaultContacts = await this.tenant.Context.Users.WithDefaultContact(contactId).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseOccurrences, Is.Zero, "a query filter is what keeps another tenant's rows out");
            Assert.That(issuedBy, Is.Zero, "a query filter is what keeps another tenant's rows out");
            Assert.That(defaultContacts, Is.Zero, "the user's tenant query filter is what keeps another tenant's rows out");
        }
    }

    [Test]
    public async Task TheDefaultContactCheckFindsTheUserThatHoldsIt()
    {
        var other = await this.tenant.AddContact("Městský úřad");

        var holdsDefault = await this.tenant.Context.Users.WithDefaultContact(this.tenant.DefaultContact.Id).CountAsync();
        var holdsOther = await this.tenant.Context.Users.WithDefaultContact(other.Id).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(holdsDefault, Is.EqualTo(1), "the check finds the user whose default the contact is");
            Assert.That(holdsOther, Is.Zero, "a contact no user prefills with is nobody's default");
        }
    }

    /// <summary>
    /// What a returned occurrence cannot show.
    /// </summary>
    [Test]
    public void AnOccurrenceCountsNothingUnderItself()
    {
        var sql = this.tenant.Context.ExternalCaseNumbers
            .AssignedByContact(Guid.CreateVersion7())
            .InCaseOccurrenceOrder()
            .AsCaseOccurrences()
            .ToQueryString();

        Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "an occurrence stands for one mark and counts nothing under it");
    }
}

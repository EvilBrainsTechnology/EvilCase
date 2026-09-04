using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class ContactOccurrenceQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 7);

    [Test]
    public async Task AnActWhoseContactDiffersFromItsCaseComesBack()
    {
        var caseContact = await this.Tenant.AddContact("Městský úřad");
        var actContact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var @case = await this.Tenant.AddCase(Day, contact: caseContact);
        var act = await this.Tenant.AddAct(@case, Day, "Rozhodnutí", contact: actContact, externalActNumber: "MUVZ/2026/117");

        var occurrences = await this.Tenant.Context.Acts
            .WithContactDifferingFromItsCase(actContact.Id)
            .AsActOccurrences()
            .ToListAsync();

        var expected = new ContactActOccurrence
        {
            ActId = act.Id,
            ActNumber = act.ActNumber,
            ActTitle = "Rozhodnutí",
            ActDate = Day,
            CaseId = @case.Id,
            CaseNumber = @case.CaseNumber,
            ExternalNumber = "MUVZ/2026/117",
        };

        ContactActOccurrence[] expectedRows = [expected];

        Assert.That(occurrences, Is.EqualTo(expectedRows), "an act occurrence carries the number another authority gave the act");
    }

    [Test]
    public async Task AnActNamingTheSameContactAsItsCaseStaysOut()
    {
        var contact = await this.Tenant.AddContact("Městský úřad");
        var @case = await this.Tenant.AddCase(Day, contact: contact);
        await this.Tenant.AddAct(@case, Day, "Rozhodnutí", contact: contact);

        var occurrences = await this.Tenant.Context.Acts
            .WithContactDifferingFromItsCase(contact.Id)
            .AsActOccurrences()
            .ToListAsync();

        Assert.That(occurrences, Is.Empty, "the case's own list already carries it");
    }

    [Test]
    public async Task AnActWhoseCaseNamesNoContactComesBack()
    {
        var actContact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var @case = await this.Tenant.AddCase(Day);
        await this.Tenant.AddAct(@case, Day, "Rozhodnutí", contact: actContact);

        var occurrences = await this.Tenant.Context.Acts
            .WithContactDifferingFromItsCase(actContact.Id)
            .AsActOccurrences()
            .ToListAsync();

        Assert.That(occurrences, Has.Count.EqualTo(1), "a case that names no contact still differs from the act's");
    }

    [Test]
    public async Task TheCasesOfTheContactComeBack()
    {
        var first = await this.Tenant.AddContact("Městský úřad");
        var second = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var mine = await this.Tenant.AddCase(Day, "Můj spis", contact: first);
        await this.Tenant.AddCase(Day, "Cizí spis", contact: second);

        var caseIds = await this.Tenant.Context.Cases
            .WithContact(first.Id)
            .Select(static @case => @case.Id)
            .ToListAsync();

        Guid[] expected = [mine.Id];

        Assert.That(caseIds, Is.EqualTo(expected), "the contact detail lists the cases naming it and no others");
    }

    [Test]
    public async Task TheNewestActComesFirst()
    {
        var caseContact = await this.Tenant.AddContact("Městský úřad");
        var actContact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var @case = await this.Tenant.AddCase(Day, contact: caseContact);

        var middle = await this.Tenant.AddAct(@case, Day.AddDays(3), "Výzva", contact: actContact);
        var newest = await this.Tenant.AddAct(@case, Day.AddDays(7), "Rozhodnutí", contact: actContact);
        var oldest = await this.Tenant.AddAct(@case, Day, "Podání", contact: actContact);

        var actIds = await this.Tenant.Context.Acts
            .WithContactDifferingFromItsCase(actContact.Id)
            .InLatestOrder()
            .Select(static act => act.Id)
            .ToListAsync();

        Guid[] expected = [newest.Id, middle.Id, oldest.Id];

        Assert.That(actIds, Is.EqualTo(expected), "the occurrences of a contact are ordered newest first");
    }

    [Test]
    public async Task AnOccurrenceOfAnotherTenantNeverComesBack()
    {
        Guid contactId;

        await using (var other = await TestTenant.Create())
        {
            var otherContact = await other.AddContact("Cizí úřad");
            var otherCase = await other.AddCase(Day, contact: otherContact);
            await other.AddAct(otherCase, Day, contact: otherContact);

            contactId = otherContact.Id;
        }

        var acts = await this.Tenant.Context.Acts.WithContactDifferingFromItsCase(contactId).CountAsync();
        var cases = await this.Tenant.Context.Cases.WithContact(contactId).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(acts, Is.Zero, "a query filter is what keeps another tenant's rows out");
            Assert.That(cases, Is.Zero, "a query filter is what keeps another tenant's rows out");
        }
    }

    [Test]
    public async Task TheDetailCarriesTheContactsCasesAndItsDifferingActs()
    {
        var caseContact = await this.Tenant.AddContact("Městský úřad");
        var actContact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var @case = await this.Tenant.AddCase(Day, contact: caseContact);
        var differing = await this.Tenant.AddAct(@case, Day, "Rozhodnutí", contact: actContact);
        await this.Tenant.AddAct(@case, Day.AddDays(1), "Podání", contact: caseContact);

        var reader = new ContactReader(new FixedDbSession(this.Tenant.Context));

        var ofActContact = await reader.GetContactDetail(actContact.Id, CancellationToken.None);
        var ofCaseContact = await reader.GetContactDetail(caseContact.Id, CancellationToken.None);

        Guid[] expectedActs = [differing.Id];
        Guid[] expectedCases = [@case.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ofActContact!.Cases, Is.Empty, "the contact names no case of its own");
            Assert.That(ofActContact.Acts.Select(static occurrence => occurrence.ActId), Is.EqualTo(expectedActs));
            Assert.That(ofCaseContact!.Cases.Select(static item => item.CaseId), Is.EqualTo(expectedCases));
            Assert.That(ofCaseContact.Acts, Is.Empty, "an act naming its case's own contact is already listed under that case");
        }
    }
}

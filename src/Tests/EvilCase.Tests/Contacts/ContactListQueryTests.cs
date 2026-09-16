using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

public class ContactListQueryTests : TenantFixture
{
    [Test]
    public async Task TheSearchFoldsCaseAndDiacriticsOverTheNameAndTheDataBoxId()
    {
        await this.Tenant.AddContact("Městský úřad Beroun");
        await this.Tenant.AddContact("Jan Novák", ContactKind.Person, dataBoxId: "úřadxy");
        await this.Tenant.AddContact("MESTSKY URAD Kladno");
        await this.Tenant.AddContact("Okresní soud", dataBoxId: "abcdefg");

        var byPlainTerm = await this.Search("urad");
        var byAccentedTerm = await this.Search("Úřad");

        string[] expected = ["Městský úřad Beroun", "Jan Novák", "MESTSKY URAD Kladno"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byPlainTerm, Is.EquivalentTo(expected), "the search folds case and diacritics over both the name and the data box id");
            Assert.That(byAccentedTerm, Is.EquivalentTo(expected), "the term folds too, so an accented term reaches a row written without diacritics");
        }
    }

    [Test]
    public async Task ABlankSearchReturnsEveryContactOfTheTenant()
    {
        await this.Tenant.AddContact("Městský úřad");
        await this.Tenant.AddContact("Okresní soud");

        var unset = await this.Tenant.Context.Contacts.MatchingSearch(search: null).CountAsync();
        var empty = await this.Tenant.Context.Contacts.MatchingSearch("").CountAsync();
        var blank = await this.Tenant.Context.Contacts.MatchingSearch("   ").CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(unset, Is.EqualTo(2), "a blank term narrows nothing");
            Assert.That(empty, Is.EqualTo(2), "a blank term narrows nothing");
            Assert.That(blank, Is.EqualTo(2), "a blank term narrows nothing");
        }
    }

    [Test]
    public async Task AWildcardInTheTermMatchesOnlyItself()
    {
        await this.Tenant.AddContact(@"Sleva 50%_a\b");
        await this.Tenant.AddContact("Sleva 50 ab");

        var names = await this.Search(@"50%_a\b");

        string[] expected = [@"Sleva 50%_a\b"];

        Assert.That(names, Is.EqualTo(expected), "a wildcard in the term matches only itself");
    }

    [Test]
    public async Task TheOrderIsByNameWithTheWriteMomentBreakingATie()
    {
        var contactIds = TestTenant.SortedEntityIds(2);

        await this.Tenant.AddContact("Zeman");
        await this.Tenant.AddContact("Adam");

        // The higher identifier is written first, so the tie shows which of the two decides.
        var written = await this.Tenant.AddContact("Novák", contactId: contactIds[1]);
        var writtenLater = await this.Tenant.AddContact("Novák", contactId: contactIds[0]);

        var names = await this.Tenant.Context.Contacts
            .InSortOrder(ContactSortKey.Name, ListSortDirection.Ascending)
            .Select(static contact => contact.Name)
            .ToListAsync();

        var tiedIds = await this.Tenant.Context.Contacts
            .Where(static contact => contact.Name == "Novák")
            .InSortOrder(ContactSortKey.Name, ListSortDirection.Ascending)
            .Select(static contact => contact.Id)
            .ToListAsync();

        string[] expectedNames = ["Adam", "Novák", "Novák", "Zeman"];
        Guid[] expectedTied = [written.Id, writtenLater.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Is.EqualTo(expectedNames), "the contact list is ordered by name");
            Assert.That(tiedIds, Is.EqualTo(expectedTied), "the write moment breaks a tie on the name");
        }
    }

    [Test]
    public async Task ARowCarriesTheKindTheNameTheDataBoxIdAndTheAddress()
    {
        var seeded = await this.Tenant.AddContact(
            "Městský úřad Beroun",
            dataBoxId: "abcdefg",
            address: "Husovo náměstí 68\n266 01 Beroun");

        var row = await this.Tenant.Context.Contacts
            .Where(contact => contact.Id == seeded.Id)
            .AsListItems()
            .SingleAsync();

        var expected = new ContactListItem
        {
            ContactId = seeded.Id,
            Kind = ContactKind.Authority,
            Name = "Městský úřad Beroun",
            DataBoxId = "abcdefg",
            Address = "Husovo náměstí 68\n266 01 Beroun",
        };

        Assert.That(row, Is.EqualTo(expected), "a row of the list shows the contact's kind, name, data box id and address");
    }

    [Test]
    public async Task AContactOfAnotherTenantNeverComesBack()
    {
        var mine = await this.Tenant.AddContact("Můj kontakt");

        await using (var other = await TestTenant.Create())
            await other.AddContact("Cizí kontakt");

        var names = await this.Tenant.Context.Contacts
            .MatchingSearch(search: null)
            .InSortOrder(ContactSortKey.Name, ListSortDirection.Ascending)
            .AsListItems()
            .Select(static item => item.Name)
            .ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain(mine.Name), "the tenant query filter keeps the tenant's own rows");
            Assert.That(names, Does.Not.Contain("Cizí kontakt"), "the tenant query filter is what keeps another tenant's rows out");
        }
    }

    [Test]
    public void TheListCountsNothingUnderARowAndPagesInTheDatabase()
    {
        var sql = this.Tenant.Context.Contacts
            .MatchingSearch(search: null)
            .InSortOrder(ContactSortKey.Name, ListSortDirection.Ascending)
            .InPage(skip: 20, take: 10)
            .AsListItems()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "a row of the list stands for one contact and counts nothing under it");
            Assert.That(sql, Does.Contain("LIMIT"), "the page is one the database applies");
            Assert.That(sql, Does.Contain("OFFSET"), "the page is one the database applies");
        }
    }

    [Test]
    public async Task OnlyTheContactsOfTheKindComeBack()
    {
        var authority = await this.Tenant.AddContact("Městský úřad");
        await this.Tenant.AddContact("Jan Novák", ContactKind.Person);

        var ids = await this.Tenant.Context.Contacts
            .WithKind(ContactKind.Authority)
            .Select(static contact => contact.Id)
            .ToListAsync();
        var whole = await this.Tenant.Context.Contacts.WithKind(kind: null).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ids, Is.EqualTo([authority.Id]), "the kind filter leaves only the contacts of that kind");
            Assert.That(whole, Is.EqualTo(2), "an absent kind narrows nothing");
        }
    }

    [Test]
    public async Task TheReaderPagesTheContactsAndCountsThemAll()
    {
        await this.Tenant.AddContact("Adam");
        await this.Tenant.AddContact("Novák");
        await this.Tenant.AddContact("Zeman");

        var reader = new ContactReader(new FixedDbSession(this.Tenant.Context));

        var page = await reader.ListContacts(
            new ContactListRequest { Skip = 1, Take = 1, SortDirection = ListSortDirection.Ascending }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(page.Items.Select(static item => item.Name), Is.EqualTo(["Novák"]), "the page starts where Skip says and holds what Take says");
            Assert.That(page.TotalCount, Is.EqualTo(3), "the total counts every contact the filter leaves");
        }
    }

    private async Task<List<string>> Search(string term)
    {
        return await this.Tenant.Context.Contacts
            .MatchingSearch(term)
            .Select(static contact => contact.Name)
            .ToListAsync();
    }
}

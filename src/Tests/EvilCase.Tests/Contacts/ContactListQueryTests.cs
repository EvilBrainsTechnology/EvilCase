using EvilBrains.EvilCase.Api.Contract.Contacts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The contact list rules on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself. Only what a result cannot show is read off the generated SQL.
/// </summary>
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

        // The tenant's user carries a default contact, which is a contact of the tenant like any other.
        using (Assert.EnterMultipleScope())
        {
            Assert.That(unset, Is.EqualTo(3), "a blank term narrows nothing");
            Assert.That(empty, Is.EqualTo(3), "a blank term narrows nothing");
            Assert.That(blank, Is.EqualTo(3), "a blank term narrows nothing");
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
    public async Task TheOrderIsByNameWithTheIdentifierBreakingATie()
    {
        var contactIds = TestTenant.SortedEntityIds(2);

        await this.Tenant.AddContact("Zeman");
        await this.Tenant.AddContact("Adam");

        // The higher identifier is written first, so the write order is not what the tie falls to.
        var secondNovak = await this.Tenant.AddContact("Novák", contactId: contactIds[1]);
        var firstNovak = await this.Tenant.AddContact("Novák", contactId: contactIds[0]);

        var names = await this.Seeded()
            .InListOrder()
            .Select(contact => contact.Name)
            .ToListAsync();

        var tiedIds = await this.Seeded()
            .Where(contact => contact.Name == "Novák")
            .InListOrder()
            .Select(contact => contact.Id)
            .ToListAsync();

        string[] expectedNames = ["Adam", "Novák", "Novák", "Zeman"];
        Guid[] expectedTied = [firstNovak.Id, secondNovak.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Is.EqualTo(expectedNames), "the contact list is ordered by name");
            Assert.That(tiedIds, Is.EqualTo(expectedTied), "the identifier only breaks a tie on the name");
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
            .InListOrder()
            .AsListItems()
            .Select(item => item.Name)
            .ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(names, Does.Contain(mine.Name), "the tenant query filter keeps the tenant's own rows");
            Assert.That(names, Does.Not.Contain("Cizí kontakt"), "the tenant query filter is what keeps another tenant's rows out");
        }
    }

    /// <summary>
    /// What a returned row cannot show.
    /// </summary>
    [Test]
    public void TheListReadsNoTimestampCountsNothingAndPagesNothing()
    {
        var sql = this.Tenant.Context.Contacts
            .MatchingSearch(search: null)
            .InListOrder()
            .AsListItems()
            .ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Not.Contain("\"Created\""), "a row of the list shows no timestamp");
            Assert.That(sql, Does.Not.Contain("count(").IgnoreCase, "a row of the list stands for one contact and counts nothing under it");
            Assert.That(sql, Does.Not.Contain("LIMIT"), "the overview has no paging");
            Assert.That(sql, Does.Not.Contain("OFFSET"), "the overview has no paging");
        }
    }

    /// <summary>
    /// The tenant's contacts without the user's default one, which every seeded tenant carries.
    /// </summary>
    private IQueryable<Contact> Seeded()
    {
        return this.Tenant.Context.Contacts.Where(contact => contact.Id != this.Tenant.DefaultContact.Id);
    }

    private async Task<List<string>> Search(string term)
    {
        return await this.Tenant.Context.Contacts
            .MatchingSearch(term)
            .Select(contact => contact.Name)
            .ToListAsync();
    }
}

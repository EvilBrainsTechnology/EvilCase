using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Business.Search;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Labels;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Search;

public class SearchReaderTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private SearchReader reader = null!;

    [SetUp]
    public void SetUpReader()
    {
        this.reader = new SearchReader(new FixedDbSession(this.Tenant.Context));
    }

    [Test]
    public async Task EveryEntityIsFoundByTheFieldItsRowShowsIgnoringCaseAndDiacritics()
    {
        var @case = await this.Tenant.AddCase(Day, "Pojištění právní ochrany", caseNumber: "EC/20260824-001");
        var act = await this.Tenant.AddAct(@case, Day, "Výzva pojišťovně");
        var contact = await this.Tenant.AddContact("Pojišťovna Alfa", dataBoxId: "pojvz12", address: "Hlavní 12, Praha");
        await this.Tenant.AddActFile(act, "pojistna-smlouva.pdf");
        await this.Tenant.AddCaseComment(@case, "Pojišťovna potvrdila přijetí");

        var response = await this.Find("POJISTENI");
        var byTitle = await this.Find("pojišťovn");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Cases.Items.Select(static item => item.CaseId), Is.EqualTo([@case.Id]), "the case title is searched");
            Assert.That(byTitle.Acts.Items.Select(static item => item.ActId), Is.EqualTo([act.Id]), "the act title is searched");
            Assert.That(byTitle.Contacts.Items.Select(static item => item.ContactId), Is.EqualTo([contact.Id]), "the contact name is searched");
            Assert.That(byTitle.Comments.Items, Has.Count.EqualTo(1), "the comment body is searched");
        }
    }

    [Test]
    public async Task AFileIsFoundOnceWithTheSizeOfItsCurrentVersion()
    {
        var @case = await this.Tenant.AddCase(Day);
        var file = await this.Tenant.AddCaseFile(@case, "verzovany-dokument.pdf", sizeBytes: 1);
        await this.Tenant.AddFileVersion(file, sizeBytes: 7);

        var response = await this.Find("verzovany");

        var item = response.Files.Items.Single();

        Assert.That(item.SizeBytes, Is.EqualTo(7), "a file is found once, with the size of its current version");
    }

    [Test]
    public async Task TheNumbersTheAddressTheDataBoxIdAndTheFileNameAreSearchedToo()
    {
        var @case = await this.Tenant.AddCase(Day, "Spis", caseNumber: "EC/20260824-007", externalCaseNumber: "JID-55/2026");
        var act = await this.Tenant.AddAct(@case, Day, "Úkon", actNumber: "EC/20260824-007/03", externalActNumber: "SZ-901");
        await this.Tenant.AddContact("Kontakt", dataBoxId: "abc1234", address: "Náměstí Míru 3");
        await this.Tenant.AddCaseFile(@case, "smlouva-2026.pdf");

        var byCaseNumber = await this.Find("20260824-007");
        var byExternalCase = await this.Find("jid-55");
        var byActNumber = await this.Find("007/03");
        var byExternalAct = await this.Find("SZ-901");
        var byAddress = await this.Find("namesti miru");
        var byDataBox = await this.Find("abc1234");
        var byFile = await this.Find("smlouva-2026");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byCaseNumber.Cases.TotalCount, Is.EqualTo(1), "the case number is searched");
            Assert.That(byExternalCase.Cases.TotalCount, Is.EqualTo(1), "the external case number is searched");
            Assert.That(byActNumber.Acts.Items.Select(static item => item.ActId), Is.EqualTo([act.Id]), "the act number is searched");
            Assert.That(byExternalAct.Acts.TotalCount, Is.EqualTo(1), "the external act number is searched");
            Assert.That(byAddress.Contacts.TotalCount, Is.EqualTo(1), "the contact address is searched");
            Assert.That(byDataBox.Contacts.TotalCount, Is.EqualTo(1), "the data box id is searched");
            Assert.That(byFile.Files.TotalCount, Is.EqualTo(1), "the file name is searched");
        }
    }

    [Test]
    public async Task AGroupCarriesTheNewestThreeRowsAndTheTotalOfAllOfThem()
    {
        var first = await this.Tenant.AddCase(new DateOnly(2026, 1, 1), "Odvolání první");
        await this.Tenant.AddCase(new DateOnly(2026, 2, 1), "Odvolání druhé");
        var third = await this.Tenant.AddCase(new DateOnly(2026, 3, 1), "Odvolání třetí");
        var fourth = await this.Tenant.AddCase(new DateOnly(2026, 4, 1), "Odvolání čtvrté");
        var fifth = await this.Tenant.AddCase(new DateOnly(2026, 5, 1), "Odvolání páté");

        var response = await this.Find("odvolani");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Cases.TotalCount, Is.EqualTo(5), "the total counts every match, not the rows shown");
            Assert.That(
                response.Cases.Items.Select(static item => item.Title),
                Is.EqualTo(["Odvolání páté", "Odvolání čtvrté", "Odvolání třetí"]),
                "a group shows three rows, the newest case first");
            Assert.That(response.Cases.Items.Select(static item => item.CaseId), Does.Not.Contain(first.Id));
            Assert.That(response.Cases.Items[0].CaseId, Is.EqualTo(fifth.Id));
            Assert.That(response.Cases.Items[1].CaseId, Is.EqualTo(fourth.Id));
            Assert.That(response.Cases.Items[2].CaseId, Is.EqualTo(third.Id));
        }
    }

    [Test]
    public async Task TheGroupsWithoutADateShowTheNewestCreatedFirst()
    {
        var @case = await this.Tenant.AddCase(Day, "Spis");
        var olderContact = await this.Tenant.AddContact("Archiv A");
        var newerContact = await this.Tenant.AddContact("Archiv B");
        await this.Tenant.AddCaseFile(@case, "archiv-a.pdf");
        await this.Tenant.AddCaseFile(@case, "archiv-b.pdf");
        await this.Tenant.AddCaseComment(@case, "Archiv A");
        await this.Tenant.AddCaseComment(@case, "Archiv B");

        var response = await this.Find("archiv");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Contacts.Items.Select(static item => item.ContactId), Is.EqualTo([newerContact.Id, olderContact.Id]), "contacts go newest created first");
            Assert.That(response.Files.Items.Select(static item => item.FileName), Is.EqualTo(["archiv-b.pdf", "archiv-a.pdf"]), "files go newest created first");
            Assert.That(response.Comments.Items.Select(static item => item.Body), Is.EqualTo(["Archiv B", "Archiv A"]), "comments go newest created first");
        }
    }

    [Test]
    public async Task ACaseRowCarriesItsStatusDateAndLabelsAndAnActRowItsDirectionAndCase()
    {
        var @case = await this.Tenant.AddCase(Day, "Odvolání");
        var label = await this.Tenant.AddLabel("Priorita", LabelColor.Pink);
        await this.Tenant.AddCaseLabel(@case, label);
        var contact = await this.Tenant.AddContact("Úřad");
        await this.Tenant.AddAct(@case, Day, "Odvolání proti rozhodnutí", contact, direction: ActDirection.Outgoing);

        var response = await this.Find("odvolani");
        var caseRow = response.Cases.Items.Single();
        var actRow = response.Acts.Items.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseRow.Labels.Select(static item => item.Name), Is.EqualTo(["Priorita"]), "the case row carries its label chips");
            Assert.That(caseRow.Date, Is.EqualTo(Day));
            Assert.That(caseRow.CaseNumber, Is.EqualTo(@case.CaseNumber));
            Assert.That(actRow.Direction, Is.EqualTo(ActDirection.Outgoing));
            Assert.That(actRow.CaseTitle, Is.EqualTo("Odvolání"), "the act row names its case");
        }
    }

    [Test]
    public async Task AFileAndACommentNameTheCaseOrTheActTheyHangOn()
    {
        var @case = await this.Tenant.AddCase(Day, "Spis o pokutě");
        var act = await this.Tenant.AddAct(@case, Day, "Úkon k pokutě");
        await this.Tenant.AddCaseFile(@case, "pokuta-spis.pdf");
        await this.Tenant.AddActFile(act, "pokuta-ukon.pdf");
        await this.Tenant.AddCaseComment(@case, "Pokuta zaplacena");
        await this.Tenant.AddActComment(act, "Pokuta napadena");

        var response = await this.Find("pokut");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                response.Files.Items.Select(static item => (item.CaseId is not null, item.ActId is not null, item.OwnerTitle)),
                Is.EquivalentTo([(true, false, "Spis o pokutě"), (false, true, "Úkon k pokutě")]),
                "a file names the owner it hangs on");
            Assert.That(
                response.Comments.Items.Select(static item => (item.CaseId is not null, item.ActId is not null, item.OwnerTitle)),
                Is.EquivalentTo([(true, false, "Spis o pokutě"), (false, true, "Úkon k pokutě")]),
                "a comment names the owner it hangs on");
        }
    }

    [Test]
    public async Task AWildcardInTheTermMatchesOnlyItself()
    {
        await this.Tenant.AddCase(Day, "Sleva 50%_a");
        await this.Tenant.AddCase(Day, "Sleva 50 ab");

        var response = await this.Find("50%_a");

        Assert.That(response.Cases.Items.Select(static item => item.Title), Is.EqualTo(["Sleva 50%_a"]), "a wildcard in the term matches only itself");
    }

    [Test]
    public async Task NothingOfAnotherTenantIsFound()
    {
        await this.Tenant.AddCase(Day, "Můj spis");

        await using (var other = await TestTenant.Create())
        {
            var otherCase = await other.AddCase(Day, "Cizí spis");
            await other.AddContact("Cizí kontakt");
            await other.AddCaseComment(otherCase, "Cizí komentář");
        }

        var response = await this.Find("cizi");
        var mine = await this.Find("muj");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                response.Cases.TotalCount + response.Contacts.TotalCount + response.Comments.TotalCount,
                Is.Zero,
                "the tenant query filter keeps another tenant's rows out of every group");
            Assert.That(mine.Cases.TotalCount, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task ATermNothingMatchesYieldsEmptyGroups()
    {
        await this.Tenant.AddCase(Day, "Odvolání");

        var response = await this.Find("zzzz");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.Cases.Items, Is.Empty);
            Assert.That(response.Cases.TotalCount, Is.Zero);
        }
    }

    [Test]
    [TestCase("")]
    [TestCase("a")]
    [TestCase("   ")]
    [TestCase(" ab")]
    [TestCase("ab ")]
    [TestCase(" ab ")]
    public void ATermUnderTwoCharactersOrWithASpaceAtEitherEndIsInvalid(string term)
    {
        Assert.That(IsValid(term), Is.False, "the term has two characters at least and no space at either end");
    }

    [Test]
    [TestCase("ab")]
    [TestCase("a b")]
    public void ATermOfTwoCharactersWithSpacesOnlyInsideIsValid(string term)
    {
        Assert.That(IsValid(term), Is.True);
    }

    private static bool IsValid(string term)
    {
        var request = new SearchRequest { Q = term };

        return Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            [],
            validateAllProperties: true);
    }

    private async Task<SearchResponse> Find(string term)
    {
        return await this.reader.Search(new SearchRequest { Q = term }, CancellationToken.None);
    }
}

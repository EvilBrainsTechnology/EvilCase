using EvilBrains.EvilCase.Business.Search;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Search;

/// <summary>
/// The search rules on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself.
/// </summary>
public class SearchQueryTests
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
    public async Task TheTextSearchFoldsCaseAndDiacriticsOverTitlesAndDescriptions()
    {
        await this.tenant.AddCase(Day, "Odvolání proti rozhodnutí");
        await this.tenant.AddCase(Day, "Přestupek", description: "Odvolání podáno v termínu");
        await this.tenant.AddCase(Day, "ODVOLANI bez diakritiky");
        await this.tenant.AddCase(Day, "Nahlédnutí do spisu", description: "bez poznámky");

        var byPlainTerm = await this.CaseTitles("odvolani");
        var byAccentedTerm = await this.CaseTitles("Odvolání");

        string[] expected = ["Odvolání proti rozhodnutí", "Přestupek", "ODVOLANI bez diakritiky"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byPlainTerm, Is.EquivalentTo(expected), "the search folds case and diacritics over both the title and the description");
            Assert.That(byAccentedTerm, Is.EquivalentTo(expected), "the term folds too, so an accented term reaches a row written without diacritics");
        }
    }

    [Test]
    public async Task APrefixOfAWordMatchesTheWholeWord()
    {
        await this.tenant.AddCase(Day, "Rozhodnutí o přestupku");
        await this.tenant.AddCase(Day, "Vyjádření k podkladům");

        var titles = await this.CaseTitles("rozhod");

        Assert.That(titles, Is.EqualTo(["Rozhodnutí o přestupku"]), "the term is a prefix, so a stem reaches the word");
    }

    [Test]
    public async Task EveryWordOfTheTermHasToMatch()
    {
        await this.tenant.AddCase(Day, "Odvolání proti rozhodnutí");
        await this.tenant.AddCase(Day, "Odvolání odmítnuto");

        var titles = await this.CaseTitles("odvolani rozhod");

        Assert.That(titles, Is.EqualTo(["Odvolání proti rozhodnutí"]), "a term of two words narrows, it does not widen");
    }

    [Test]
    public async Task AFragmentOfTheCaseNumberMatchesTheCase()
    {
        await this.tenant.AddCase(Day, "Přestupek");

        var titles = await this.CaseTitles("20260824-001");

        Assert.That(titles, Is.EqualTo(["Přestupek"]), "a fragment of the case number reaches the case");
    }

    [Test]
    public async Task AFragmentOfAnExternalMarkMatchesTheCase()
    {
        var contact = await this.tenant.AddContact("Úřad");
        var wanted = await this.tenant.AddCase(Day, "Přestupek");
        await this.tenant.AddExternalCaseNumber(wanted, "MUVZ/2025/80535", contact);
        await this.tenant.AddCase(Day, "Jiná věc");

        var titles = await this.CaseTitles("80535");

        Assert.That(titles, Is.EqualTo(["Přestupek"]), "a fragment of an external mark reaches the case");
    }

    [Test]
    public async Task AnActMatchesItsTitleItsNumberAndItsExternalNumber()
    {
        var @case = await this.tenant.AddCase(Day, "Přestupek");
        var contact = await this.tenant.AddContact("Úřad");
        var act = await this.tenant.AddAct(@case, Day, "Příkaz o uložení pokuty");
        await this.tenant.AddExternalActNumber(act, "MUVZ/2025/93547", contact);
        await this.tenant.AddAct(@case, Day, "Vyjádření");

        var byTitle = await this.ActIds("prikaz");
        var byOwnNumber = await this.ActIds(act.ActNumber);
        var byExternalNumber = await this.ActIds("93547");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byTitle, Is.EqualTo([act.Id]), "the act is reached by its title, by its own number and by an external one");
            Assert.That(byOwnNumber, Is.EqualTo([act.Id]), "the act is reached by its title, by its own number and by an external one");
            Assert.That(byExternalNumber, Is.EqualTo([act.Id]), "the act is reached by its title, by its own number and by an external one");
        }
    }

    [Test]
    public async Task ATermOfOnlyPunctuationFindsNothing()
    {
        await this.tenant.AddCase(Day, "Přestupek");

        var titles = await this.CaseTitles("...");

        Assert.That(titles, Is.Empty, "a term with no letter and no digit reaches no text and no number");
    }

    [Test]
    public async Task ARowOfAnotherTenantNeverComesBack()
    {
        await this.tenant.AddCase(Day, "Odvolání");

        await using (var other = await TestTenant.Create())
            await other.AddCase(Day, "Odvolání");

        var titles = await this.CaseTitles("odvolani");

        Assert.That(titles, Has.Count.EqualTo(1), "the tenant query filter is what keeps another tenant's rows out");
    }

    [Test]
    public async Task TheSearchOrderIsTheDateNewestFirstWithTheNumberBreakingATie()
    {
        var older = await this.tenant.AddCase(new DateOnly(2026, 8, 20), "Odvolání starší");
        var caseIds = TestTenant.SortedEntityIds(2);
        var second = await this.tenant.AddCase(Day, "Odvolání druhé", caseId: caseIds[1]);
        var first = await this.tenant.AddCase(Day, "Odvolání první", caseId: caseIds[0]);

        var ordered = await this.tenant.Context.Cases
            .MatchingTerm("odvolani")
            .InSearchOrder()
            .Select(@case => @case.Id)
            .ToListAsync();

        Guid[] expected = [.. new[] { first, second }.OrderBy(@case => @case.CaseNumber, StringComparer.Ordinal).Select(@case => @case.Id), older.Id];

        Assert.That(ordered, Is.EqualTo(expected), "the newest date leads and the number breaks a tie on it");
    }

    [Test]
    public void TheTextBranchIsIndexedAndTheNumberBranchReadsTheColumn()
    {
        var sql = this.tenant.Context.Cases.MatchingTerm("odvolani").ToQueryString();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(sql, Does.Contain("to_tsvector("), "the text branch is a full-text match, not a LIKE over the text");
            Assert.That(sql, Does.Contain("immutable_unaccent("), "the fold happens in the database, on the expression the Init indexes carry");
            Assert.That(sql, Does.Contain("\"CaseNumber\" ILIKE"), "the number branch reads the column itself, so its trigram index applies");
        }
    }

    private async Task<List<string>> CaseTitles(string term)
    {
        return await this.tenant.Context.Cases
            .MatchingTerm(term)
            .Select(@case => @case.Title)
            .ToListAsync();
    }

    private async Task<List<Guid>> ActIds(string term)
    {
        return await this.tenant.Context.Acts
            .MatchingTerm(term)
            .Select(act => act.Id)
            .ToListAsync();
    }
}

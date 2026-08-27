using EvilBrains.EvilCase.Api.Contract.Search;
using EvilBrains.EvilCase.Business.Search;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Search;

/// <summary>
/// The combined search and the exact match it resolves, on the rows a real PostgreSQL returns. Each
/// test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class SearchReaderTests
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private TestTenant tenant = null!;

    private SearchReader reader = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.reader = new SearchReader(new FixedDbSession(this.tenant.Context));
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task TheResultHoldsCasesAndActsTogether()
    {
        var @case = await this.tenant.AddCase(Day, "Odvolání proti rozhodnutí");
        var act = await this.tenant.AddAct(@case, Day, "Odvolání v plném rozsahu");

        var response = await this.Search("odvolani");

        Assert.That(response.Items, Has.Count.EqualTo(2));

        var caseItem = response.Items.Single(item => item.Kind == SearchResultKind.Case);
        var actItem = response.Items.Single(item => item.Kind == SearchResultKind.Act);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(caseItem.CaseId, Is.EqualTo(@case.Id));
            Assert.That(caseItem.ActId, Is.Null, "a case names no act");
            Assert.That(caseItem.Number, Is.EqualTo(@case.CaseNumber));
            Assert.That(actItem.CaseId, Is.EqualTo(@case.Id), "an act names the case it belongs to, so the result knows where it navigates");
            Assert.That(actItem.ActId, Is.EqualTo(act.Id));
            Assert.That(actItem.Number, Is.EqualTo(act.ActNumber));
        }
    }

    [Test]
    public async Task AtMostTenResultsComeBack()
    {
        var @case = await this.tenant.AddCase(Day, "Odvolání");
        foreach (var index in Enumerable.Range(1, 12))
            await this.tenant.AddAct(@case, Day, string.Create(CultureInfo.InvariantCulture, $"Odvolání {index}"));

        var response = await this.Search("odvolani");

        Assert.That(response.Items, Has.Count.EqualTo(10), "the drop-down shows at most ten results");
    }

    [Test]
    public async Task TheNewestComesFirstAcrossCasesAndActs()
    {
        var host = await this.tenant.AddCase(new DateOnly(2026, 8, 20), "Spis");
        var olderCase = await this.tenant.AddCase(new DateOnly(2026, 8, 20), "Odvolání spis");
        var act = await this.tenant.AddAct(host, Day, "Odvolání úkon");

        var response = await this.Search("odvolani");

        Assert.That(response.Items.Select(item => item.Number), Is.EqualTo([act.ActNumber, olderCase.CaseNumber]), "the newest date leads, whichever kind carries it");
    }

    [Test]
    public async Task AnExactCaseNumberNavigatesToItsCase()
    {
        var @case = await this.tenant.AddCase(Day, "Přestupek");

        var response = await this.Search(@case.CaseNumber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.ExactMatch?.Kind, Is.EqualTo(SearchResultKind.Case), "an exact own number navigates straight to the case");
            Assert.That(response.ExactMatch?.CaseId, Is.EqualTo(@case.Id));
        }
    }

    [Test]
    public async Task AnExactActNumberNavigatesToItsAct()
    {
        var @case = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(@case, Day, "Úkon");

        var response = await this.Search(act.ActNumber);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.ExactMatch?.Kind, Is.EqualTo(SearchResultKind.Act), "an exact own number navigates straight to the act");
            Assert.That(response.ExactMatch?.ActId, Is.EqualTo(act.Id));
            Assert.That(response.ExactMatch?.CaseId, Is.EqualTo(@case.Id));
        }
    }

    [Test]
    public async Task AnOwnNumberInAnotherLetterCaseStillNavigates()
    {
        var @case = await this.tenant.AddCase(Day, "Přestupek");

        var response = await this.Search(@case.CaseNumber.ToLowerInvariant());

        Assert.That(response.ExactMatch?.CaseId, Is.EqualTo(@case.Id), "the exact match ignores letter case");
    }

    [Test]
    public async Task AnExternalNumberOnOneEntityNavigatesToIt()
    {
        var contact = await this.tenant.AddContact("Úřad");
        var @case = await this.tenant.AddCase(Day, "Přestupek");
        await this.tenant.AddExternalCaseNumber(@case, "MUVZ/2025/80535", contact);

        var response = await this.Search("MUVZ/2025/80535");

        Assert.That(response.ExactMatch?.CaseId, Is.EqualTo(@case.Id));
    }

    [Test]
    public async Task AnExternalNumberOnTwoEntitiesNavigatesNowhere()
    {
        var contact = await this.tenant.AddContact("Úřad");
        var @case = await this.tenant.AddCase(Day, "Přestupek");
        var act = await this.tenant.AddAct(@case, Day, "Úkon");
        await this.tenant.AddExternalCaseNumber(@case, "10 A 1/2025", contact);
        await this.tenant.AddExternalActNumber(act, "10 A 1/2025", contact);

        var response = await this.Search("10 A 1/2025");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(response.ExactMatch, Is.Null, "an external number two entities carry navigates nowhere");
            Assert.That(response.Items, Is.Not.Empty, "the results are shown instead");
        }
    }

    [Test]
    public async Task AnOwnNumberBeatsAnExternalOneOnAnotherEntity()
    {
        var contact = await this.tenant.AddContact("Úřad");
        var a = await this.tenant.AddCase(Day, "Případ A");
        var b = await this.tenant.AddCase(Day, "Případ B");
        await this.tenant.AddExternalCaseNumber(b, a.CaseNumber, contact);

        var response = await this.Search(a.CaseNumber);

        Assert.That(response.ExactMatch?.CaseId, Is.EqualTo(a.Id), "an own number wins over an external one that repeats it");
    }

    [Test]
    public async Task ATermShorterThanTwoCharactersFindsNothing()
    {
        await this.tenant.AddCase(Day, "Odvolání");

        var byOneCharacter = await this.Search("o");
        var byEmpty = await this.Search("");
        var byBlank = await this.Search("  ");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byOneCharacter.Items, Is.Empty, "the search runs from two characters");
            Assert.That(byOneCharacter.ExactMatch, Is.Null, "the search runs from two characters");
            Assert.That(byEmpty.Items, Is.Empty, "the search runs from two characters");
            Assert.That(byEmpty.ExactMatch, Is.Null, "the search runs from two characters");
            Assert.That(byBlank.Items, Is.Empty, "the search runs from two characters");
            Assert.That(byBlank.ExactMatch, Is.Null, "the search runs from two characters");
        }
    }

    [Test]
    public async Task NothingOfAnotherTenantComesBack()
    {
        var mine = await this.tenant.AddCase(Day, "Odvolání");

        await using (var other = await TestTenant.Create())
            await other.AddCase(Day, "Odvolání");

        var response = await this.Search("odvolani");

        Assert.That(response.Items.Select(item => item.CaseId), Is.EqualTo([mine.Id]));
    }

    private Task<SearchResponse> Search(string query)
    {
        return this.reader.Search(new SearchRequest { Query = query }, CancellationToken.None);
    }
}

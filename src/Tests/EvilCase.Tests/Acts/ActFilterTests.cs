using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActFilterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheSearchFoldsCaseAndDiacriticsOverTheActsTitleAndDescription()
    {
        var @case = await this.Tenant.AddCase(Day);

        await this.Tenant.AddAct(@case, Day, "Odvolání proti rozhodnutí");
        await this.Tenant.AddAct(@case, Day, "Výzva", description: "Odvolání podáno v termínu");
        await this.Tenant.AddAct(@case, Day, "ODVOLANI bez diakritiky");
        await this.Tenant.AddAct(@case, Day, "Nahlédnutí do spisu", description: "bez poznámky");

        var byPlainTerm = await this.Titles("odvolani");
        var byAccentedTerm = await this.Titles("Odvolání");

        string[] expected = ["Odvolání proti rozhodnutí", "Výzva", "ODVOLANI bez diakritiky"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(byPlainTerm, Is.EquivalentTo(expected), "the search folds case and diacritics over both the title and the description");
            Assert.That(byAccentedTerm, Is.EquivalentTo(expected), "the term folds too, so an accented term reaches a row written without diacritics");
        }
    }

    [Test]
    public async Task OnlyTheActsOfTheCaseComeBack()
    {
        var first = await this.Tenant.AddCase(Day);
        var second = await this.Tenant.AddCase(Day);
        var wanted = await this.Tenant.AddAct(first, Day, "Podání");
        await this.Tenant.AddAct(second, Day, "Jiný úkon");

        var ids = await this.Tenant.Context.Acts.OfCase(first.Id).Select(static act => act.Id).ToListAsync();

        Guid[] expected = [wanted.Id];

        Assert.That(ids, Is.EqualTo(expected), "the case filter never reaches into another case");
    }

    [Test]
    public async Task EveryActOfTheContactComesBackEvenWhereItsCaseNamesTheSameContact()
    {
        var contact = await this.Tenant.AddContact("Městský úřad Vzorov");
        var ownCase = await this.Tenant.AddCase(Day, "Spis kontaktu", contact: contact);
        var otherCase = await this.Tenant.AddCase(Day, "Cizí spis");

        var inOwnCase = await this.Tenant.AddAct(ownCase, Day, "Rozhodnutí", contact: contact);
        var inOtherCase = await this.Tenant.AddAct(otherCase, Day, "Výzva", contact: contact);
        await this.Tenant.AddAct(otherCase, Day, "Bez kontaktu");

        var ids = await this.Tenant.Context.Acts.WithContact(contact.Id).Select(static act => act.Id).ToListAsync();

        Assert.That(
            ids,
            Is.EquivalentTo([inOwnCase.Id, inOtherCase.Id]),
            "the contact filter leaves every act naming it, whether or not its case names the same contact");
    }

    [Test]
    public async Task OnlyTheActsOfTheDirectionComeBack()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Městský úřad Vzorov");

        var incoming = await this.Tenant.AddAct(@case, Day, "Přijato", contact: contact, direction: ActDirection.Incoming);
        await this.Tenant.AddAct(@case, Day, "Odesláno", contact: contact, direction: ActDirection.Outgoing);
        await this.Tenant.AddAct(@case, Day, "Bez směru");

        var ids = await this.Tenant.Context.Acts.WithDirection(ActDirection.Incoming).Select(static act => act.Id).ToListAsync();

        Guid[] expected = [incoming.Id];

        Assert.That(ids, Is.EqualTo(expected), "the direction filter leaves only the acts carrying it");
    }

    [Test]
    public async Task OnlyTheActsCarryingEveryRequestedLabelComeBack()
    {
        var urgent = await this.Tenant.AddLabel("Urgentní");
        var appeal = await this.Tenant.AddLabel("Odvolání");
        var @case = await this.Tenant.AddCase(Day);

        var both = await this.Tenant.AddAct(@case, Day, "Obojí");
        var onlyOne = await this.Tenant.AddAct(@case, Day, "Jen jeden");
        await this.Tenant.AddAct(@case, Day, "Bez štítku");

        await this.Tenant.AddActLabel(both, urgent);
        await this.Tenant.AddActLabel(both, appeal);
        await this.Tenant.AddActLabel(onlyOne, urgent);

        var ids = await this.Tenant.Context.Acts
            .WithLabels([urgent.Id, appeal.Id])
            .Select(static act => act.Id)
            .ToListAsync();

        Guid[] expected = [both.Id];

        Assert.That(ids, Is.EqualTo(expected), "several labels narrow together: an act carries every one of them or it is out");
    }

    [Test]
    public async Task TheDateRangeKeepsTheActsStandingOnItsBounds()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 19));

        await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 19), "Před");
        var first = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 20), "Na dolní hranici");
        var middle = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 21), "Uvnitř");
        var last = await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 22), "Na horní hranici");
        await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 23), "Po");

        var ids = await this.Tenant.Context.Acts
            .WithinDates(new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 22))
            .OrderBy(static act => act.Date)
            .Select(static act => act.Id)
            .ToListAsync();

        Guid[] expected = [first.Id, middle.Id, last.Id];

        Assert.That(ids, Is.EqualTo(expected), "the range holds both of its bounds");
    }

    private async Task<List<string>> Titles(string search)
    {
        return await this.Tenant.Context.Acts
            .MatchingSearch(search)
            .Select(static act => act.Title)
            .ToListAsync();
    }
}

using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CaseFilterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheParentFilterDecidesAloneAndTheScopeIsIgnoredBesideIt()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);

        var besideRootOnly = await this.UnderParent(parent.Id, CaseListScope.RootOnly);
        var besideAll = await this.UnderParent(parent.Id, CaseListScope.All);

        Guid[] expected = [child.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(besideRootOnly, Is.EqualTo(expected), "a named parent decides alone and the scope beside it changes nothing");
            Assert.That(besideAll, Is.EqualTo(expected), "a named parent decides alone and the scope beside it changes nothing");
        }
    }

    [Test]
    public async Task OnlyTheDirectChildrenOfTheParentComeBack()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: root.Id);
        await this.Tenant.AddCase(Day, "Vnuk", parentCaseId: child.Id);

        var children = await this.UnderParent(root.Id, CaseListScope.All);

        Guid[] expected = [child.Id];

        Assert.That(children, Is.EqualTo(expected), "the parent filter reaches one level and never a whole tree");
    }

    [Test]
    public async Task OnlyTheCasesOfTheContactComeBack()
    {
        var contact = await this.Tenant.AddContact("Městský úřad Vzorov");
        var wanted = await this.Tenant.AddCase(Day, "S kontaktem", contact: contact);
        await this.Tenant.AddCase(Day, "Bez kontaktu");

        var ids = await this.Tenant.Context.Cases.WithContact(contact.Id).Select(static @case => @case.Id).ToListAsync();

        Guid[] expected = [wanted.Id];

        Assert.That(ids, Is.EqualTo(expected), "the contact filter leaves only the cases naming it");
    }

    [Test]
    public async Task OnlyTheCasesCarryingEveryRequestedLabelComeBack()
    {
        var urgent = await this.Tenant.AddLabel("Urgentní");
        var appeal = await this.Tenant.AddLabel("Odvolání");

        var both = await this.Tenant.AddCase(Day, "Obojí");
        var onlyOne = await this.Tenant.AddCase(Day, "Jen jeden");
        await this.Tenant.AddCase(Day, "Bez štítku");

        await this.Tenant.AddCaseLabel(both, urgent);
        await this.Tenant.AddCaseLabel(both, appeal);
        await this.Tenant.AddCaseLabel(onlyOne, urgent);

        var ids = await this.Tenant.Context.Cases
            .WithLabels([urgent.Id, appeal.Id])
            .Select(static @case => @case.Id)
            .ToListAsync();

        Guid[] expected = [both.Id];

        Assert.That(ids, Is.EqualTo(expected), "several labels narrow together: a case carries every one of them or it is out");
    }

    [Test]
    public async Task TheDateRangeKeepsTheCasesStandingOnItsBounds()
    {
        await this.Tenant.AddCase(new DateOnly(2026, 8, 19), "Před");
        var first = await this.Tenant.AddCase(new DateOnly(2026, 8, 20), "Na dolní hranici");
        var middle = await this.Tenant.AddCase(new DateOnly(2026, 8, 21), "Uvnitř");
        var last = await this.Tenant.AddCase(new DateOnly(2026, 8, 22), "Na horní hranici");
        await this.Tenant.AddCase(new DateOnly(2026, 8, 23), "Po");

        var ids = await this.Tenant.Context.Cases
            .WithinDates(new DateOnly(2026, 8, 20), new DateOnly(2026, 8, 22))
            .OrderBy(static @case => @case.Date)
            .Select(static @case => @case.Id)
            .ToListAsync();

        Guid[] expected = [first.Id, middle.Id, last.Id];

        Assert.That(ids, Is.EqualTo(expected), "the range holds both of its bounds");
    }

    [Test]
    public async Task AnAbsentFilterNarrowsNothing()
    {
        await this.Tenant.AddCase(Day, "První");
        await this.Tenant.AddCase(Day, "Druhý");

        var withoutContact = await this.Tenant.Context.Cases.WithContact(contactId: null).CountAsync();
        var withoutLabels = await this.Tenant.Context.Cases.WithLabels([]).CountAsync();
        var withoutDates = await this.Tenant.Context.Cases.WithinDates(from: null, to: null).CountAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(withoutContact, Is.EqualTo(2), "an absent contact narrows nothing");
            Assert.That(withoutLabels, Is.EqualTo(2), "an absent label narrows nothing");
            Assert.That(withoutDates, Is.EqualTo(2), "an absent date range narrows nothing");
        }
    }

    private async Task<List<Guid>> UnderParent(Guid? parentCaseId, CaseListScope scope)
    {
        return await this.Tenant.Context.Cases
            .UnderParent(parentCaseId, scope)
            .Select(static @case => @case.Id)
            .ToListAsync();
    }
}

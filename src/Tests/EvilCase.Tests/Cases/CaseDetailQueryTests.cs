using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The one case's header, on the rows a real PostgreSQL returns. Each test seeds a tenant of its own,
/// so none cleans up after itself.
/// </summary>
public class CaseDetailQueryTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task TheDetailCarriesTheNumberTheDateTheTitleTheDescriptionAndTheStatus()
    {
        var @case = await this.Tenant.AddCase(
            Day,
            "Přestupek",
            description: "Popis přestupku",
            status: CaseStatus.WaitingOnAuthority);

        var detail = await this.Tenant.Context.Cases.DetailOf(@case.Id, CancellationToken.None);

        Assert.That(detail, Is.Not.Null);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.CaseId, Is.EqualTo(@case.Id));
            Assert.That(detail.CaseNumber, Is.EqualTo(@case.CaseNumber));
            Assert.That(detail.Date, Is.EqualTo(Day));
            Assert.That(detail.Title, Is.EqualTo("Přestupek"));
            Assert.That(detail.Description, Is.EqualTo("Popis přestupku"));
            Assert.That(detail.Status, Is.EqualTo(CaseStatus.WaitingOnAuthority));
        }
    }

    [Test]
    public async Task ACaseWithNoParentNamesNone()
    {
        var @case = await this.Tenant.AddCase(Day);

        var detail = await this.Tenant.Context.Cases.DetailOf(@case.Id, CancellationToken.None);

        Assert.That(detail!.ParentCase, Is.Null);
    }

    [Test]
    public async Task TheDetailNamesTheParentCase()
    {
        var parent = await this.Tenant.AddCase(Day, "Nadřízený");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: parent.Id);

        var detail = await this.Tenant.Context.Cases.DetailOf(child.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.ParentCase!.CaseId, Is.EqualTo(parent.Id), "the detail links to the parent so the flat lists are enough to walk the hierarchy");
            Assert.That(detail.ParentCase.CaseNumber, Is.EqualTo(parent.CaseNumber));
            Assert.That(detail.ParentCase.Title, Is.EqualTo(parent.Title));
        }
    }

    [Test]
    public async Task TheDetailListsOnlyTheDirectSubordinateCases()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: root.Id);
        _ = await this.Tenant.AddCase(Day, "Vnuk", parentCaseId: child.Id);

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));
        var detail = await reader.GetCaseDetail(root.Id, CancellationToken.None);

        Assert.That(detail!.ChildCases.Select(static item => item.CaseId), Is.EqualTo([child.Id]), "the detail lists the direct subordinates and never a whole tree");
    }

    [Test]
    public async Task TheSubordinateCasesComeNewestFirst()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var older = await this.Tenant.AddCase(Day.AddDays(-2), "Starší", parentCaseId: root.Id);
        var newer = await this.Tenant.AddCase(Day.AddDays(-1), "Novější", parentCaseId: root.Id);

        var reader = new CaseReader(new FixedDbSession(this.Tenant.Context));
        var detail = await reader.GetCaseDetail(root.Id, CancellationToken.None);

        Assert.That(
            detail!.ChildCases.Select(static item => item.CaseId),
            Is.EqualTo([newer.Id, older.Id]),
            "subordinate cases share the list order, newest by the case's own date first");
    }

    [Test]
    public async Task TheDetailCarriesTheCasesExternalMark()
    {
        var @case = await this.Tenant.AddCase(Day, externalCaseNumber: "VV41/2025/08464");

        var detail = await this.Tenant.Context.Cases.DetailOf(@case.Id, CancellationToken.None);

        Assert.That(detail!.ExternalCaseNumber, Is.EqualTo("VV41/2025/08464"));
    }

    [Test]
    public async Task ACaseWithNoExternalMarkNamesNone()
    {
        var @case = await this.Tenant.AddCase(Day);

        var detail = await this.Tenant.Context.Cases.DetailOf(@case.Id, CancellationToken.None);

        Assert.That(detail!.ExternalCaseNumber, Is.Null, "the mark is optional and its absence is no mark, not a failed read");
    }

    [Test]
    public async Task AnUnknownIdIsNoDetail()
    {
        var detail = await this.Tenant.Context.Cases.DetailOf(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNoDetail()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);

        var detail = await this.Tenant.Context.Cases.DetailOf(otherCase.Id, CancellationToken.None);

        Assert.That(detail, Is.Null, "the tenant query filter is what turns another tenant's id into nothing");
    }
}

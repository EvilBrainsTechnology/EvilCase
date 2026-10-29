using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Cases;

public class CasePathTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    [Test]
    public async Task ARootCaseHasNoAncestors()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");

        var detail = await new CaseReader(new FixedDbSession(this.Tenant.Context)).GetCaseDetail(root.Id, CancellationToken.None);

        Assert.That(detail!.Ancestors, Is.Empty);
    }

    [Test]
    public async Task ACaseNamesEveryCaseAboveItTheRootFirst()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var middle = await this.Tenant.AddCase(Day, "Prostřední", parentCaseId: root.Id);
        var leaf = await this.Tenant.AddCase(Day, "List", parentCaseId: middle.Id);

        var detail = await new CaseReader(new FixedDbSession(this.Tenant.Context)).GetCaseDetail(leaf.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail!.Ancestors.Select(static crumb => crumb.CaseId), Is.EqualTo([root.Id, middle.Id]));
            Assert.That(detail.Ancestors.Select(static crumb => crumb.Title), Is.EqualTo(["Kořen", "Prostřední"]));
        }
    }

    [Test]
    public async Task AnActNamesEveryCaseAboveItsCaseTheRootFirst()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var middle = await this.Tenant.AddCase(Day, "Prostřední", parentCaseId: root.Id);
        var leaf = await this.Tenant.AddCase(Day, "List", parentCaseId: middle.Id);
        var act = await this.Tenant.AddAct(leaf, Day);

        var detail = await new ActReader(new FixedDbSession(this.Tenant.Context)).GetActDetail(act.Id, CancellationToken.None);

        Assert.That(detail!.CaseAncestors.Select(static crumb => crumb.CaseId), Is.EqualTo([root.Id, middle.Id]));
    }
}

using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Domain.Labels;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Labels;

public class LabelListQueryTests : TenantFixture
{
    private LabelReader reader = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpReader()
    {
        this.reader = new LabelReader(new FixedDbSession(this.Tenant.Context));
    }

    [Test]
    public async Task TheListReadsByNameAndCarriesEveryColour()
    {
        await this.Tenant.AddLabel("Soud", LabelColor.Indigo);
        await this.Tenant.AddLabel("Hlídat lhůtu", LabelColor.Orange);
        await this.Tenant.AddLabel("Priorita", LabelColor.Red);

        var items = await this.reader.ListLabels(CancellationToken.None);

        string[] expected = ["Hlídat lhůtu", "Priorita", "Soud"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(items.Select(static item => item.Name), Is.EqualTo(expected), "the list reads by name");
            Assert.That(items.Single(static item => string.Equals(item.Name, "Soud", StringComparison.Ordinal)).Color, Is.EqualTo(LabelColor.Indigo));
        }
    }

    [Test]
    public async Task EachLabelCountsTheCasesAndActsThatCarryIt()
    {
        var used = await this.Tenant.AddLabel("Soud");
        await this.Tenant.AddLabel("Priorita");

        var first = await this.Tenant.AddCase(new DateOnly(2026, 8, 1));
        var second = await this.Tenant.AddCase(new DateOnly(2026, 8, 2));
        var act = await this.Tenant.AddAct(first, new DateOnly(2026, 8, 3));

        await this.Tenant.AddCaseLabel(first, used);
        await this.Tenant.AddCaseLabel(second, used);
        await this.Tenant.AddActLabel(act, used);

        var items = await this.reader.ListLabels(CancellationToken.None);

        Assert.That(items.Select(static item => (item.Name, item.CaseCount, item.ActCount)), Is.EqualTo([("Priorita", 0, 0), ("Soud", 2, 1)]));
    }
}

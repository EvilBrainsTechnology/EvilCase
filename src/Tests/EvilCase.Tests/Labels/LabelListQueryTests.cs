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
}

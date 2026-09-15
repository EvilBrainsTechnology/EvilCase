using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Domain.Labels;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Labels;

public class LabelWriterTests : TenantFixture
{
    private LabelWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new LabelWriter(new FixedDbSession(this.Tenant.Context), NullLogger<LabelWriter>.Instance);
    }

    [Test]
    public async Task ACreatedLabelCarriesItsNameAndColour()
    {
        var request = new LabelEditRequest { Name = "  Hlídat lhůtu  ", Color = LabelColor.Orange };

        var result = await this.writer.CreateLabel(request, CancellationToken.None);

        var reloaded = await this.Tenant.Context.Labels.SingleAsync(label => label.Id == result.Label!.LabelId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(LabelCreateOutcome.Created));
            Assert.That(result.Label?.Name, Is.EqualTo("Hlídat lhůtu"), "a name is stored without its surrounding space");
            Assert.That(result.Label?.Color, Is.EqualTo(LabelColor.Orange));
            Assert.That(reloaded.Name, Is.EqualTo("Hlídat lhůtu"));
            Assert.That(reloaded.Color, Is.EqualTo(LabelColor.Orange));
        }
    }

    [Test]
    public async Task ANameAnotherLabelAlreadyCarriesIsRefused()
    {
        await this.Tenant.AddLabel("Soud");

        var result = await this.writer.CreateLabel(new LabelEditRequest { Name = "Soud", Color = LabelColor.Indigo }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(LabelCreateOutcome.NameTaken));
            Assert.That(await this.Tenant.Context.Labels.CountAsync(), Is.EqualTo(1), "the refused create writes no second row");
        }
    }

    [Test]
    public async Task AnEditRewritesTheNameAndTheColour()
    {
        var label = await this.Tenant.AddLabel("Soud", LabelColor.Indigo);

        var outcome = await this.writer.UpdateLabel(label.Id, new LabelEditRequest { Name = "Krajský soud", Color = LabelColor.Purple }, CancellationToken.None);

        var reloaded = await this.Tenant.Context.Labels.SingleAsync(row => row.Id == label.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(LabelUpdateOutcome.Updated));
            Assert.That(reloaded.Name, Is.EqualTo("Krajský soud"));
            Assert.That(reloaded.Color, Is.EqualTo(LabelColor.Purple));
        }
    }

    [Test]
    public async Task AnEditKeepingItsOwnNameIsNotTakenByItself()
    {
        var label = await this.Tenant.AddLabel("Soud", LabelColor.Indigo);

        var outcome = await this.writer.UpdateLabel(label.Id, new LabelEditRequest { Name = "Soud", Color = LabelColor.Red }, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(LabelUpdateOutcome.Updated), "a label keeps its own name, so only another label's name is taken");
    }

    [Test]
    public async Task AnEditIntoANameAnotherLabelCarriesIsRefused()
    {
        await this.Tenant.AddLabel("Soud");
        var label = await this.Tenant.AddLabel("Priorita");

        var outcome = await this.writer.UpdateLabel(label.Id, new LabelEditRequest { Name = "Soud", Color = LabelColor.Red }, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(LabelUpdateOutcome.NameTaken));
    }

    [Test]
    public async Task AnEditOfALabelThatIsNotThereIsNotFound()
    {
        var outcome = await this.writer.UpdateLabel(Guid.CreateVersion7(), new LabelEditRequest { Name = "Soud", Color = LabelColor.Red }, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(LabelUpdateOutcome.NotFound));
    }

    [Test]
    public async Task ADeletedLabelIsTakenOffEverythingThatCarriedIt()
    {
        var label = await this.Tenant.AddLabel("Soud");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 1, 6));

        await this.Tenant.AddCaseLabel(@case, label);
        await this.Tenant.AddActLabel(act, label);

        var outcome = await this.writer.DeleteLabel(label.Id, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(await this.Tenant.Context.LabelAssignments.CountAsync(), Is.Zero, "the cascade takes the label off the case and the act");
            Assert.That(await this.Tenant.Context.Cases.CountAsync(), Is.EqualTo(1), "the case outlives the label it carried");
            Assert.That(await this.Tenant.Context.Acts.CountAsync(), Is.EqualTo(1), "the act outlives the label it carried");
        }
    }

    [Test]
    public async Task ADeleteOfALabelThatIsNotThereIsNotFound()
    {
        var outcome = await this.writer.DeleteLabel(Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(DeleteOutcome.NotFound));
    }
}

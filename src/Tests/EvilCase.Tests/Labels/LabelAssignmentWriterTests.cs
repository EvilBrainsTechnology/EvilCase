using EvilBrains.EvilCase.Api.Contract.Labels;
using EvilBrains.EvilCase.Business.Labels;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Labels;

public class LabelAssignmentWriterTests : TenantFixture
{
    private LabelAssignmentWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new LabelAssignmentWriter(new FixedDbSession(this.Tenant.Context), NullLogger<LabelAssignmentWriter>.Instance);
    }

    [Test]
    public async Task TheSetSentIsTheSetTheCaseCarriesAfterwards()
    {
        var kept = await this.Tenant.AddLabel("Priorita");
        var dropped = await this.Tenant.AddLabel("Soud");
        var added = await this.Tenant.AddLabel("Hlídat lhůtu");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));

        await this.Tenant.AddCaseLabel(@case, kept);
        await this.Tenant.AddCaseLabel(@case, dropped);

        var outcome = await this.writer.SetCaseLabels(@case.Id, new LabelAssignmentRequest { LabelIds = [kept.Id, added.Id] }, CancellationToken.None);

        var carried = await this.CarriedLabelIds();

        Guid[] expected = [kept.Id, added.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(LabelAssignmentOutcome.Assigned));
            Assert.That(carried, Is.EquivalentTo(expected), "a label left out of the set is taken off");
        }
    }

    [Test]
    public async Task ASetSentTwiceLeavesOneRowPerLabel()
    {
        var label = await this.Tenant.AddLabel("Priorita");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));
        var request = new LabelAssignmentRequest { LabelIds = [label.Id, label.Id] };

        await this.writer.SetCaseLabels(@case.Id, request, CancellationToken.None);
        await this.writer.SetCaseLabels(@case.Id, request, CancellationToken.None);

        Assert.That(await this.Tenant.Context.LabelAssignments.CountAsync(), Is.EqualTo(1), "a repeated label id assigns the label once");
    }

    [Test]
    public async Task AnEmptySetTakesEveryLabelOff()
    {
        var label = await this.Tenant.AddLabel("Priorita");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));

        await this.Tenant.AddCaseLabel(@case, label);

        await this.writer.SetCaseLabels(@case.Id, new LabelAssignmentRequest(), CancellationToken.None);

        Assert.That(await this.Tenant.Context.LabelAssignments.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task ALabelTheTenantDoesNotHaveIsRefusedAndWritesNothing()
    {
        var label = await this.Tenant.AddLabel("Priorita");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));

        await this.Tenant.AddCaseLabel(@case, label);

        var outcome = await this.writer.SetCaseLabels(
            @case.Id,
            new LabelAssignmentRequest { LabelIds = [Guid.CreateVersion7()] },
            CancellationToken.None);

        var carried = await this.CarriedLabelIds();

        Guid[] expected = [label.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(LabelAssignmentOutcome.LabelNotFound));
            Assert.That(carried, Is.EqualTo(expected), "the refused set leaves what the case carried alone");
        }
    }

    [Test]
    public async Task ACaseTheTenantDoesNotHaveIsNotFound()
    {
        var outcome = await this.writer.SetCaseLabels(Guid.CreateVersion7(), new LabelAssignmentRequest(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(LabelAssignmentOutcome.OwnerNotFound));
    }

    [Test]
    public async Task TheSetSentIsTheSetTheActCarriesAfterwards()
    {
        var label = await this.Tenant.AddLabel("Soud");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 1, 6));

        var outcome = await this.writer.SetActLabels(@case.Id, act.Id, new LabelAssignmentRequest { LabelIds = [label.Id] }, CancellationToken.None);

        var assignment = await this.Tenant.Context.LabelAssignments.SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(LabelAssignmentOutcome.Assigned));
            Assert.That(assignment.ActId, Is.EqualTo(act.Id));
            Assert.That(assignment.CaseId, Is.Null, "an assignment hangs on the act alone");
            Assert.That(assignment.LabelId, Is.EqualTo(label.Id));
        }
    }

    [Test]
    public async Task AnActUnderAnotherCaseIsNotFound()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));
        var other = await this.Tenant.AddCase(new DateOnly(2026, 1, 7));
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 1, 6));

        var outcome = await this.writer.SetActLabels(other.Id, act.Id, new LabelAssignmentRequest(), CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(LabelAssignmentOutcome.OwnerNotFound));
    }

    private async Task<List<Guid>> CarriedLabelIds()
    {
        return await this.Tenant.Context.LabelAssignments
            .Select(static assignment => assignment.LabelId)
            .ToListAsync();
    }
}

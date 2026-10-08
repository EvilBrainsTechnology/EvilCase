using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActTimelineTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    /// <summary>
    /// The day offsets of seven acts in the order they are written, which is not the date order.
    /// </summary>
    private static readonly int[] WriteOrder = [3, 0, 6, 1, 5, 2, 4];

    [Test]
    public async Task TheFirstActStandsFirstAndSeesTwoActsAhead()
    {
        var (caseId, actIds) = await this.AddCaseOfSevenActs();

        var detail = await this.ReadActDetail(caseId, actIds[0]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.ActPosition, Is.EqualTo(1));
            Assert.That(detail.CaseActCount, Is.EqualTo(7));
            Assert.That(
                detail.CaseTimeline.Select(static item => item.ActId),
                Is.EqualTo(actIds[..3]),
                "the first act has no act before it, so the timeline runs two acts ahead");
            Assert.That(
                detail.CaseTimeline.Single(static item => item.IsCurrent).ActId,
                Is.EqualTo(actIds[0]),
                "exactly the act read is the current one");
        }
    }

    [Test]
    public async Task AnActInTheMiddleSeesTwoActsEitherSide()
    {
        var (caseId, actIds) = await this.AddCaseOfSevenActs();

        var detail = await this.ReadActDetail(caseId, actIds[3]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.ActPosition, Is.EqualTo(4));
            Assert.That(detail.CaseActCount, Is.EqualTo(7));
            Assert.That(detail.CaseTimeline.Select(static item => item.ActId), Is.EqualTo(actIds[1..6]));
            Assert.That(detail.CaseTimeline.Single(static item => item.IsCurrent).ActId, Is.EqualTo(actIds[3]));
        }
    }

    [Test]
    public async Task TheLastActStandsLastAndSeesTwoActsBehind()
    {
        var (caseId, actIds) = await this.AddCaseOfSevenActs();

        var detail = await this.ReadActDetail(caseId, actIds[6]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.ActPosition, Is.EqualTo(7));
            Assert.That(detail.CaseActCount, Is.EqualTo(7));
            Assert.That(
                detail.CaseTimeline.Select(static item => item.ActId),
                Is.EqualTo(actIds[4..]),
                "the last act has no act after it, so the timeline runs two acts back");
        }
    }

    [Test]
    public async Task TheTimelineItemCarriesTheTitleTheDateAndTheDirection()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(
            @case,
            Day,
            "Odvolání",
            contact: await this.Tenant.AddContact("Městský úřad"),
            direction: ActDirection.Outgoing);

        var detail = await this.ReadActDetail(@case.Id, act.Id);
        var item = detail.CaseTimeline.Single();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.ActPosition, Is.EqualTo(1));
            Assert.That(detail.CaseActCount, Is.EqualTo(1));
            Assert.That(item.ActId, Is.EqualTo(act.Id));
            Assert.That(item.Title, Is.EqualTo("Odvolání"));
            Assert.That(item.Date, Is.EqualTo(Day));
            Assert.That(item.Direction, Is.EqualTo(ActDirection.Outgoing));
            Assert.That(item.IsCurrent, Is.True);
        }
    }

    [Test]
    public async Task EqualDatesFallBackToTheWriteMoment()
    {
        var actIds = TestTenant.SortedEntityIds(3);
        var @case = await this.Tenant.AddCase(Day);

        // The write order and the identifier order disagree, so only the write moment can break the tie.
        var first = await this.Tenant.AddAct(@case, Day, "Podání", actId: actIds[2]);
        var second = await this.Tenant.AddAct(@case, Day, "Výzva", actId: actIds[0]);
        var third = await this.Tenant.AddAct(@case, Day, "Rozhodnutí", actId: actIds[1]);

        var detail = await this.ReadActDetail(@case.Id, second.Id);

        Guid[] expected = [first.Id, second.Id, third.Id];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.ActPosition, Is.EqualTo(2), "equal act dates fall back to when the row was written");
            Assert.That(
                detail.CaseTimeline.Select(static item => item.ActId),
                Is.EqualTo(expected),
                "equal act dates fall back to when the row was written");
        }
    }

    [Test]
    public async Task AnotherCasesAndAnotherTenantsActsNeverEnterTheTimeline()
    {
        await using var other = await TestTenant.Create();

        var @case = await this.Tenant.AddCase(Day, "Náš spis");
        var otherCase = await this.Tenant.AddCase(Day, "Jiný spis");
        var first = await this.Tenant.AddAct(@case, Day);
        await this.Tenant.AddAct(otherCase, Day.AddDays(1));
        var last = await this.Tenant.AddAct(@case, Day.AddDays(2));
        var foreignCase = await other.AddCase(Day);
        await other.AddAct(foreignCase, Day.AddDays(1));

        var detail = await this.ReadActDetail(@case.Id, first.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(detail.CaseActCount, Is.EqualTo(2), "the count is the act's own case in the reader's tenant");
            Assert.That(
                detail.CaseTimeline.Select(static item => item.ActId),
                Is.EqualTo([first.Id, last.Id]),
                "another case's or another tenant's act never stands between two acts of this case");
        }
    }

    [Test]
    public async Task TheReaderReturnsNothingForAnUnknownAct()
    {
        var @case = await this.Tenant.AddCase(Day);
        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        var detail = await reader.GetActDetail(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(detail, Is.Null);
    }

    private async Task<(Guid CaseId, Guid[] ActIds)> AddCaseOfSevenActs()
    {
        var @case = await this.Tenant.AddCase(Day);
        var actIds = new Guid[WriteOrder.Length];

        foreach (var offset in WriteOrder)
            actIds[offset] = (await this.Tenant.AddAct(@case, Day.AddDays(offset))).Id;

        return (@case.Id, actIds);
    }

    private async Task<ActDetail> ReadActDetail(Guid caseId, Guid actId)
    {
        var reader = new ActReader(new FixedDbSession(this.Tenant.Context));

        return (await reader.GetActDetail(caseId, actId, CancellationToken.None))!;
    }
}

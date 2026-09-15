using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Labels;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Labels;

public class CaseAndActLabelTests : TenantFixture
{
    private CaseReader caseReader = null!;

    private CaseWriter caseWriter = null!;

    private ActReader actReader = null!;

    private ActWriter actWriter = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpReadersAndWriters()
    {
        var dbSession = new FixedDbSession(this.Tenant.Context);

        this.caseReader = new CaseReader(dbSession);
        this.caseWriter = new CaseWriter(dbSession, new FakeCaseNumberIssuer(), NullLogger<CaseWriter>.Instance);
        this.actReader = new ActReader(dbSession);
        this.actWriter = new ActWriter(dbSession, new FakeActNumberIssuer(), NullLogger<ActWriter>.Instance);
    }

    [Test]
    public async Task AFiledCaseCarriesTheLabelsItWasFiledWith()
    {
        var label = await this.Tenant.AddLabel("Priorita", LabelColor.Red);

        var result = await this.caseWriter.CreateCase(
            new CreateCaseRequest { Date = new DateOnly(2026, 1, 5), Title = "Spis", LabelIds = [label.Id] },
            CancellationToken.None);

        var detail = await this.caseReader.GetCaseDetail(result.Case!.CaseId, CancellationToken.None);

        string[] expected = ["Priorita"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(CaseCreateOutcome.Created));
            Assert.That(result.Case?.Labels.Select(static item => item.Name), Is.EqualTo(expected), "the answer to a create already carries the labels");
            Assert.That(detail?.Labels.Select(static item => item.Name), Is.EqualTo(expected));
            Assert.That(detail?.Labels[0].Color, Is.EqualTo(LabelColor.Red));
        }
    }

    [Test]
    public async Task ALabelTheTenantDoesNotHaveRefusesTheFiling()
    {
        var result = await this.caseWriter.CreateCase(
            new CreateCaseRequest { Date = new DateOnly(2026, 1, 5), Title = "Spis", LabelIds = [Guid.CreateVersion7()] },
            CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(CaseCreateOutcome.LabelNotFound));
    }

    [Test]
    public async Task AnEditRewritesTheLabelsOfTheCase()
    {
        var dropped = await this.Tenant.AddLabel("Soud");
        var added = await this.Tenant.AddLabel("Priorita");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));

        await this.Tenant.AddCaseLabel(@case, dropped);

        var outcome = await this.caseWriter.UpdateCase(
            @case.Id,
            new CaseEditRequest
            {
                CaseNumber = @case.CaseNumber,
                Date = @case.Date,
                Title = @case.Title,
                Status = CaseStatus.Active,
                LabelIds = [added.Id],
            },
            CancellationToken.None);

        var detail = await this.caseReader.GetCaseDetail(@case.Id, CancellationToken.None);

        string[] expected = ["Priorita"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(detail?.Labels.Select(static item => item.Name), Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task TheCaseListCarriesTheLabelsByName()
    {
        var second = await this.Tenant.AddLabel("Soud");
        var first = await this.Tenant.AddLabel("Priorita");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));

        await this.Tenant.AddCaseLabel(@case, second);
        await this.Tenant.AddCaseLabel(@case, first);

        var items = await this.caseReader.ListCases(new CaseListRequest(), CancellationToken.None);

        string[] expected = ["Priorita", "Soud"];

        Assert.That(items.Single().Labels.Select(static item => item.Name), Is.EqualTo(expected), "labels of a listed case read by name");
    }

    [Test]
    public async Task AFiledActCarriesTheLabelsItWasFiledWith()
    {
        var label = await this.Tenant.AddLabel("Soud", LabelColor.Indigo);
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));

        var result = await this.actWriter.CreateAct(
            @case.Id,
            new CreateActRequest { Date = new DateOnly(2026, 1, 6), Title = "Úkon", LabelIds = [label.Id] },
            CancellationToken.None);

        var detail = await this.actReader.GetActDetail(@case.Id, result.Act!.ActId, CancellationToken.None);

        string[] expected = ["Soud"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.Created));
            Assert.That(result.Act?.Labels.Select(static item => item.Name), Is.EqualTo(expected), "the answer to a create already carries the labels");
            Assert.That(detail?.Labels.Select(static item => item.Name), Is.EqualTo(expected));
            Assert.That(detail?.Labels[0].Color, Is.EqualTo(LabelColor.Indigo));
        }
    }

    [Test]
    public async Task AnEditRewritesTheLabelsOfTheAct()
    {
        var dropped = await this.Tenant.AddLabel("Soud");
        var added = await this.Tenant.AddLabel("Priorita");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 1, 6));

        await this.Tenant.AddActLabel(act, dropped);

        var outcome = await this.actWriter.UpdateAct(
            @case.Id,
            act.Id,
            new ActEditRequest { ActNumber = act.ActNumber, Date = act.Date, Title = act.Title, LabelIds = [added.Id] },
            CancellationToken.None);

        var detail = await this.actReader.GetActDetail(@case.Id, act.Id, CancellationToken.None);

        string[] expected = ["Priorita"];

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.Updated));
            Assert.That(detail?.Labels.Select(static item => item.Name), Is.EqualTo(expected));
        }
    }

    [Test]
    public async Task AnActEditNamingALabelTheTenantDoesNotHaveIsRefused()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5));
        var act = await this.Tenant.AddAct(@case, new DateOnly(2026, 1, 6));

        var outcome = await this.actWriter.UpdateAct(
            @case.Id,
            act.Id,
            new ActEditRequest { ActNumber = act.ActNumber, Date = act.Date, Title = act.Title, LabelIds = [Guid.CreateVersion7()] },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.LabelNotFound));
    }
}

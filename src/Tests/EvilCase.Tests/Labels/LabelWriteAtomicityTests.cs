using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Labels;

/// <summary>
/// A trigger fails the insert of one label's assignment, after the row it belongs to is written.
/// </summary>
public class LabelWriteAtomicityTests : TenantFixture
{
    private const string FailingTrigger = "fail_label_assignment_insert";

    private CaseWriter caseWriter = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriters()
    {
        var dbSession = new TransactionalDbSession(this.Tenant.Context);

        this.caseWriter = new CaseWriter(dbSession, new FakeCaseNumberIssuer(), NullLogger<CaseWriter>.Instance);
    }

    [TearDown]
    public async Task DropFailingTrigger()
    {
        await this.Tenant.Context.Database.ExecuteSqlRawAsync(
            $"""
            DROP TRIGGER IF EXISTS {FailingTrigger} ON "LabelAssignments";
            DROP FUNCTION IF EXISTS {FailingTrigger}();
            """);
    }

    [Test]
    public async Task AFailedLabelWriteFilesNoCase()
    {
        var label = await this.FailingLabel();

        await Assert.ThrowsAsync<DbUpdateException>(async () => await this.caseWriter.CreateCase(
            new CreateCaseRequest { Date = new DateOnly(2026, 1, 5), Title = "Nezaložený", LabelIds = [label] },
            CancellationToken.None));

        Assert.That(await this.Tenant.Context.Cases.AnyAsync(static @case => @case.Title == "Nezaložený"), Is.False);
    }

    [Test]
    public async Task AFailedLabelWriteUndoesTheCaseEdit()
    {
        var held = await this.Tenant.AddLabel("Soud");
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 1, 5), "Původní");

        await this.Tenant.AddCaseLabel(@case, held);

        var label = await this.FailingLabel();

        await Assert.ThrowsAsync<DbUpdateException>(async () => await this.caseWriter.UpdateCase(
            @case.Id,
            new CaseEditRequest
            {
                CaseNumber = @case.CaseNumber,
                Date = @case.Date,
                Title = "Nový",
                Status = CaseStatus.Active,
                LabelIds = [label],
            },
            CancellationToken.None));

        var title = await this.Tenant.Context.Cases.Where(x => x.Id == @case.Id).Select(static x => x.Title).SingleAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(title, Is.EqualTo("Původní"));
            Assert.That(await this.HeldLabels(@case.Id), Is.EqualTo([held.Id]));
        }
    }

    private async Task<Guid> FailingLabel()
    {
        var label = await this.Tenant.AddLabel("Selhávající");

        var sql = $"""
            CREATE FUNCTION {FailingTrigger}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                RAISE EXCEPTION 'injected failure';
            END
            $$;
            CREATE TRIGGER {FailingTrigger} BEFORE INSERT ON "LabelAssignments"
            FOR EACH ROW WHEN (NEW."LabelId" = '{label.Id}') EXECUTE FUNCTION {FailingTrigger}();
            """;

        await this.Tenant.Context.Database.ExecuteSqlRawAsync(sql);

        return label.Id;
    }

    private async Task<List<Guid>> HeldLabels(Guid caseId)
    {
        return await this.Tenant.Context.LabelAssignments
            .Where(x => x.CaseId == caseId)
            .Select(static x => x.LabelId)
            .ToListAsync();
    }
}

using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// The edit rules on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself.
/// </summary>
public class CaseUpdateTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private TestTenant tenant = null!;

    private CaseWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.writer = new CaseWriter(new FixedDbSession(this.tenant.Context), new FakeCaseNumberIssuer(), NullLogger<CaseWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task AnEditWritesTheDateTheTitleTheDescriptionAndTheStatus()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek", status: CaseStatus.Active);
        var request = Edit(seeded.CaseNumber, new DateOnly(2026, 9, 1), "Nový název", "Nový popis", CaseStatus.WaitingOnAuthority);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloaded.Date, Is.EqualTo(request.Date));
            Assert.That(reloaded.Title, Is.EqualTo(request.Title));
            Assert.That(reloaded.Description, Is.EqualTo(request.Description));
            Assert.That(reloaded.Status, Is.EqualTo(request.Status));
        }
    }

    [Test]
    public async Task AClosedCaseIsStillEditable()
    {
        var seeded = await this.tenant.AddCase(Day, status: CaseStatus.Closed);
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated), "the status is a label only, so a closed case takes an edit like any other");
    }

    [Test]
    public async Task ChangingTheDateLeavesTheNumberAsItWasIssued()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var newDate = Day.AddMonths(1);
        var request = Edit(seeded.CaseNumber, newDate, seeded.Title, description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloaded.CaseNumber, Is.EqualTo(seeded.CaseNumber), "moving a case does not re-issue its number");
            Assert.That(reloaded.Date, Is.EqualTo(newDate));
        }
    }

    [Test]
    public async Task ACaseKeepsItsOwnNumberOnAnEdit()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated), "a case does not take its own number from itself");
    }

    [Test]
    public async Task AHandWrittenNumberInTheFormatBecomesTheCasesOwn()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var request = Edit("  EC/20260101-042  ", Day, seeded.Title, description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloaded.CaseNumber, Is.EqualTo("EC/20260101-042"), "a hand-written number in the format replaces the issued one");
        }
    }

    [Test]
    public async Task AHandWrittenNumberOutsideTheFormatIsRefused()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var request = Edit("spis 7/2026", Day, "Jiný název", description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.InvalidCaseNumber));
            Assert.That(reloaded.CaseNumber, Is.EqualTo(seeded.CaseNumber));
            Assert.That(reloaded.Title, Is.EqualTo(seeded.Title));
        }
    }

    [Test]
    public async Task ANumberAnotherCaseHoldsIsRefused()
    {
        var first = await this.tenant.AddCase(Day, "První");
        var second = await this.tenant.AddCase(Day, "Druhý");
        var request = Edit(second.CaseNumber, Day, "Přejmenováno", description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(first.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(first.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.CaseNumberTaken), "a number another case holds is refused");
            Assert.That(reloaded.CaseNumber, Is.EqualTo(first.CaseNumber));
            Assert.That(reloaded.Title, Is.EqualTo(first.Title));
        }
    }

    [Test]
    public async Task ABlankDescriptionIsFiledAsNothing()
    {
        var seeded = await this.tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, "   ", CaseStatus.Active);

        await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        Assert.That(reloaded.Description, Is.Null);
    }

    [Test]
    public async Task AnUnknownCaseIsNotFound()
    {
        var request = Edit("EC/20260821-999", Day, "Neexistující", description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(Guid.CreateVersion7(), request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.NotFound));
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day, "Cizí spis");
        var request = Edit(otherCase.CaseNumber, Day, "Přejmenováno", description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(otherCase.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.NotFound), "the tenant query filter is what keeps another tenant's row out of an edit");
    }

    private async Task<Case> Reload(Guid caseId)
    {
        this.tenant.Context.ChangeTracker.Clear();

        return await this.tenant.Context.Cases.SingleAsync(@case => @case.Id == caseId);
    }

    private static CaseEditRequest Edit(string caseNumber, DateOnly date, string title, string? description, CaseStatus status)
    {
        return new()
        {
            CaseNumber = caseNumber,
            Date = date,
            Title = title,
            Description = description,
            Status = status,
        };
    }
}

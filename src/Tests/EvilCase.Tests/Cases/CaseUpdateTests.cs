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
public class CaseUpdateTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private CaseWriter writer = null!;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new CaseWriter(
            new FixedDbSession(this.Tenant.Context), new FakeCaseNumberIssuer(), new FakeFileBlobStore(), NullLogger<CaseWriter>.Instance);
    }

    [Test]
    public async Task AnEditWritesTheDateTheTitleTheDescriptionAndTheStatus()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek", status: CaseStatus.Active);
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
        var seeded = await this.Tenant.AddCase(Day, status: CaseStatus.Closed);
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated), "the status is a label only, so a closed case takes an edit like any other");
    }

    [Test]
    public async Task ChangingTheDateLeavesTheNumberAsItWasIssued()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
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
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated), "a case does not take its own number from itself");
    }

    [Test]
    public async Task AHandWrittenNumberInTheFormatBecomesTheCasesOwn()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
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
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
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
        var first = await this.Tenant.AddCase(Day, "První");
        var second = await this.Tenant.AddCase(Day, "Druhý");
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
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
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
    public async Task AnUnknownCaseWithAMalformedNumberIsNotFoundNotInvalid()
    {
        var request = Edit("spis 7/2026", Day, "Neexistující", description: null, CaseStatus.Active);

        var outcome = await this.writer.UpdateCase(Guid.CreateVersion7(), request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.NotFound), "the row is checked before the number is validated (R-025)");
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
        this.Tenant.Context.ChangeTracker.Clear();

        return await this.Tenant.Context.Cases.SingleAsync(@case => @case.Id == caseId);
    }

    [Test]
    public async Task AnEditHangsTheCaseUnderAParent()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active, parentCaseId: parent.Id);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloaded.ParentCaseId, Is.EqualTo(parent.Id));
        }
    }

    [Test]
    public async Task AnEditClearsTheParent()
    {
        var parent = await this.Tenant.AddCase(Day, "Rodič");
        var seeded = await this.Tenant.AddCase(Day, "Přestupek", parentCaseId: parent.Id);
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active, parentCaseId: null);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloaded.ParentCaseId, Is.Null, "the parent is optional, so an edit takes it away as well as sets it");
        }
    }

    [Test]
    public async Task ACaseCannotBecomeItsOwnParent()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active, parentCaseId: seeded.Id);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.InvalidParent));
            Assert.That(reloaded.ParentCaseId, Is.Null);
        }
    }

    [Test]
    public async Task ACaseCannotHangUnderItsOwnSubordinate()
    {
        var root = await this.Tenant.AddCase(Day, "Kořen");
        var child = await this.Tenant.AddCase(Day, "Podřízený", parentCaseId: root.Id);
        var grandchild = await this.Tenant.AddCase(Day, "Vnuk", parentCaseId: child.Id);
        var request = Edit(root.CaseNumber, Day, root.Title, description: null, CaseStatus.Active, parentCaseId: grandchild.Id);

        var outcome = await this.writer.UpdateCase(root.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(root.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.InvalidParent), "a cycle in the hierarchy is refused by the business layer");
            Assert.That(reloaded.ParentCaseId, Is.Null);
        }
    }

    [Test]
    public async Task AParentOfAnotherTenantIsRefused()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day, "Cizí spis");
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active, parentCaseId: otherCase.Id);

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.InvalidParent), "the tenant query filter is what keeps another tenant's case from becoming a parent");
            Assert.That(reloaded.ParentCaseId, Is.Null);
        }
    }

    [Test]
    public async Task AnEditWritesTheExternalMark()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active, externalCaseNumber: "  VV41/2025/08464  ");

        var outcome = await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloaded.ExternalCaseNumber, Is.EqualTo("VV41/2025/08464"), "the mark is stored without its surrounding space");
        }
    }

    [Test]
    public async Task ABlankExternalMarkIsFiledAsNothing()
    {
        var seeded = await this.Tenant.AddCase(Day, "Přestupek", externalCaseNumber: "VV41/2025/08464");
        var request = Edit(seeded.CaseNumber, Day, seeded.Title, description: null, CaseStatus.Active, externalCaseNumber: "   ");

        await this.writer.UpdateCase(seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        Assert.That(reloaded.ExternalCaseNumber, Is.Null, "an emptied field takes the mark away");
    }

    private static CaseEditRequest Edit(
        string caseNumber, DateOnly date, string title, string? description, CaseStatus status, Guid? parentCaseId = null, string? externalCaseNumber = null)
    {
        return new()
        {
            ParentCaseId = parentCaseId,
            CaseNumber = caseNumber,
            ExternalCaseNumber = externalCaseNumber,
            Date = date,
            Title = title,
            Description = description,
            Status = status,
        };
    }
}

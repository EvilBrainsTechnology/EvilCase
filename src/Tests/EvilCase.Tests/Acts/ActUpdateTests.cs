using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// The edit rules on the rows a real PostgreSQL returns. Each test seeds a tenant of its own, so none
/// cleans up after itself.
/// </summary>
public class ActUpdateTests
{
    private static readonly DateOnly Day = new(2026, 8, 21);

    private TestTenant tenant = null!;

    private ActWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create();
        this.writer = new ActWriter(new FixedDbSession(this.tenant.Context), new FakeActNumberIssuer(), new FakeFileBlobStore(), NullLogger<ActWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task AnEditWritesTheDirectionTheDateTheTitleTheDescriptionAndBothContacts()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day, "Podání");
        var recipient = await this.tenant.AddContact("Příjemce");
        var request = Edit(seeded.ActNumber, ActDirection.Outgoing, Day.AddDays(1), "Nový název", "Nový popis", seeded.IssuedByContactId, recipient.Id);

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.Updated));
            Assert.That(reloaded.Direction, Is.EqualTo(request.Direction));
            Assert.That(reloaded.Date, Is.EqualTo(request.Date));
            Assert.That(reloaded.Title, Is.EqualTo(request.Title));
            Assert.That(reloaded.Description, Is.EqualTo(request.Description));
            Assert.That(reloaded.IssuedByContactId, Is.EqualTo(request.IssuedByContactId));
            Assert.That(reloaded.AddressedToContactId, Is.EqualTo(recipient.Id));
        }
    }

    [Test]
    public async Task AnEditClearsTheRecipient()
    {
        var @case = await this.tenant.AddCase(Day);
        var recipient = await this.tenant.AddContact("Příjemce");
        var seeded = await this.tenant.AddAct(@case, Day, addressedTo: recipient);
        var request = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, seeded.Title, description: null, seeded.IssuedByContactId, addressedToContactId: null);

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.Updated));
            Assert.That(reloaded.AddressedToContactId, Is.Null, "a recipient is optional, so an edit takes it away as well as sets it");
        }
    }

    [Test]
    public async Task ChangingTheDateLeavesTheNumberAsItWasIssued()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day, "Podání");
        var newDate = Day.AddDays(3);
        var request = Edit(seeded.ActNumber, seeded.Direction, newDate, seeded.Title, description: null, seeded.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.Updated));
            Assert.That(reloaded.ActNumber, Is.EqualTo(seeded.ActNumber), "moving an act does not re-issue its number");
            Assert.That(reloaded.Date, Is.EqualTo(newDate));
        }
    }

    [Test]
    public async Task AnActKeepsItsOwnNumberOnAnEdit()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day);
        var request = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, seeded.Title, description: null, seeded.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.Updated), "an act does not take its own number from itself");
    }

    [Test]
    public async Task AHandWrittenNumberInTheFormatBecomesTheActsOwn()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day);
        var request = Edit(
            "  EC/20260101-042/20260105-007  ", seeded.Direction, seeded.Date, seeded.Title, description: null, seeded.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.Updated));
            Assert.That(
                reloaded.ActNumber,
                Is.EqualTo("EC/20260101-042/20260105-007"),
                "a hand-written number in the format replaces the issued one");
        }
    }

    [Test]
    public async Task AHandWrittenNumberOutsideTheFormatIsRefused()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day, "Podání");
        var request = Edit("cj 7/2026", seeded.Direction, seeded.Date, "Jiný název", description: null, seeded.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.InvalidActNumber));
            Assert.That(reloaded.ActNumber, Is.EqualTo(seeded.ActNumber));
            Assert.That(reloaded.Title, Is.EqualTo(seeded.Title));
        }
    }

    [Test]
    public async Task ANumberAnotherActHoldsIsRefused()
    {
        var @case = await this.tenant.AddCase(Day);
        var first = await this.tenant.AddAct(@case, Day, "První");
        var second = await this.tenant.AddAct(@case, Day, "Druhý");
        var request = Edit(second.ActNumber, first.Direction, first.Date, "Jiný název", description: null, first.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(@case.Id, first.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(first.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.ActNumberTaken));
            Assert.That(reloaded.ActNumber, Is.EqualTo(first.ActNumber));
            Assert.That(reloaded.Title, Is.EqualTo(first.Title));
        }
    }

    [Test]
    public async Task ANumberAnotherCasesActHoldsIsRefused()
    {
        var caseA = await this.tenant.AddCase(Day, "A");
        var caseB = await this.tenant.AddCase(Day, "B");
        var actA = await this.tenant.AddAct(caseA, Day);
        var actB = await this.tenant.AddAct(caseB, Day);
        var request = Edit(actB.ActNumber, actA.Direction, actA.Date, actA.Title, description: null, actA.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(caseA.Id, actA.Id, request, CancellationToken.None);

        Assert.That(
            outcome,
            Is.EqualTo(ActUpdateOutcome.ActNumberTaken),
            "an act number is unique across the whole tenant, not only inside its case");
    }

    [Test]
    public async Task ABlankDescriptionIsFiledAsNothing()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day);
        var request = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, seeded.Title, "   ", seeded.IssuedByContactId);

        await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        Assert.That(reloaded.Description, Is.Null);
    }

    [Test]
    public async Task AnEditNamingAContactThatIsNotThereIsRefused()
    {
        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day, "Podání");
        var request = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, "Jiný název", description: null, Guid.CreateVersion7());

        var outcome = await this.writer.UpdateAct(@case.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.ContactNotFound));
            Assert.That(reloaded.Title, Is.EqualTo(seeded.Title));
            Assert.That(reloaded.IssuedByContactId, Is.EqualTo(seeded.IssuedByContactId));
        }
    }

    [Test]
    public async Task AnEditNamingTheContactOfAnotherTenantIsRefused()
    {
        await using var other = await TestTenant.Create();
        var foreignContact = await other.AddContact("Cizí kontakt");

        var @case = await this.tenant.AddCase(Day);
        var seeded = await this.tenant.AddAct(@case, Day);

        var asSender = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, seeded.Title, description: null, foreignContact.Id);
        var senderOutcome = await this.writer.UpdateAct(@case.Id, seeded.Id, asSender, CancellationToken.None);

        var asRecipient = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, seeded.Title, description: null, seeded.IssuedByContactId, foreignContact.Id);
        var recipientOutcome = await this.writer.UpdateAct(@case.Id, seeded.Id, asRecipient, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(senderOutcome, Is.EqualTo(ActUpdateOutcome.ContactNotFound), "an act never names a contact of another tenant");
            Assert.That(recipientOutcome, Is.EqualTo(ActUpdateOutcome.ContactNotFound), "an act never names a contact of another tenant");
        }
    }

    [Test]
    public async Task AnUnknownActIsNotFound()
    {
        var @case = await this.tenant.AddCase(Day);
        var request = Edit("EC/20260101-042/20260105-007", ActDirection.Incoming, Day, "Podání", description: null, this.tenant.DefaultContact.Id);

        var outcome = await this.writer.UpdateAct(@case.Id, Guid.CreateVersion7(), request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.NotFound));
    }

    [Test]
    public async Task AnUnknownActIsNotFoundBeforeTheEditIsWeighed()
    {
        var @case = await this.tenant.AddCase(Day);
        var request = Edit("cj 7/2026", ActDirection.Incoming, Day, "Podání", description: null, Guid.CreateVersion7());

        var outcome = await this.writer.UpdateAct(@case.Id, Guid.CreateVersion7(), request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.NotFound), "an act that is not there is not found, whatever else the edit gets wrong");
    }

    [Test]
    public async Task AnActOfAnotherCaseIsNotFound()
    {
        var first = await this.tenant.AddCase(Day, "První");
        var second = await this.tenant.AddCase(Day, "Druhý");
        var seeded = await this.tenant.AddAct(first, Day, "Podání");
        var request = Edit(seeded.ActNumber, seeded.Direction, seeded.Date, "Jiný název", description: null, seeded.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(second.Id, seeded.Id, request, CancellationToken.None);

        var reloaded = await this.Reload(seeded.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.NotFound), "an act is only ever edited under the case it sits in");
            Assert.That(reloaded.Title, Is.EqualTo(seeded.Title));
        }
    }

    [Test]
    public async Task AnActOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        var request = Edit(otherAct.ActNumber, otherAct.Direction, otherAct.Date, otherAct.Title, description: null, otherAct.IssuedByContactId);

        var outcome = await this.writer.UpdateAct(otherCase.Id, otherAct.Id, request, CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ActUpdateOutcome.NotFound), "the tenant query filter is what keeps another tenant's row out of an edit");
    }

    [Test]
    public async Task RewritingTheCaseNumberLeavesTheIssuedActNumbersAlone()
    {
        var @case = await this.tenant.AddCase(Day);
        var first = await this.tenant.AddAct(@case, Day, "První");
        var second = await this.tenant.AddAct(@case, Day, "Druhý");
        var firstNumber = first.ActNumber;
        var secondNumber = second.ActNumber;

        var caseWriter = new CaseWriter(new FixedDbSession(this.tenant.Context), new FakeCaseNumberIssuer(), new FakeFileBlobStore(), NullLogger<CaseWriter>.Instance);
        var caseEdit = new CaseEditRequest
        {
            CaseNumber = "EC/20260101-042",
            Date = @case.Date,
            Title = @case.Title,
            Description = @case.Description,
            Status = CaseStatus.Active,
        };

        var outcome = await caseWriter.UpdateCase(@case.Id, caseEdit, CancellationToken.None);

        this.tenant.Context.ChangeTracker.Clear();
        var reloadedCase = await this.tenant.Context.Cases.SingleAsync(c => c.Id == @case.Id);
        var reloadedFirst = await this.Reload(first.Id);
        var reloadedSecond = await this.Reload(second.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(CaseUpdateOutcome.Updated));
            Assert.That(reloadedCase.CaseNumber, Is.EqualTo("EC/20260101-042"));
            Assert.That(
                reloadedFirst.ActNumber,
                Is.EqualTo(firstNumber),
                "rewriting a case number never rewrites the act numbers already issued under it (SDD-008)");
            Assert.That(
                reloadedSecond.ActNumber,
                Is.EqualTo(secondNumber),
                "rewriting a case number never rewrites the act numbers already issued under it (SDD-008)");
        }
    }

    private async Task<Act> Reload(Guid actId)
    {
        this.tenant.Context.ChangeTracker.Clear();

        return await this.tenant.Context.Acts.SingleAsync(act => act.Id == actId);
    }

    private static ActEditRequest Edit(
        string actNumber, ActDirection direction, DateOnly date, string title, string? description, Guid issuedByContactId, Guid? addressedToContactId = null)
    {
        return new()
        {
            ActNumber = actNumber,
            Direction = direction,
            Date = date,
            Title = title,
            Description = description,
            IssuedByContactId = issuedByContactId,
            AddressedToContactId = addressedToContactId,
        };
    }
}

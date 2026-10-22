using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Numbering;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActWriterTests : TenantFixture
{
    protected override bool AsHost => true;

    [Test]
    public void ANewActCarriesTheRequestAndTheNumberIssuedToIt()
    {
        var caseId = Guid.CreateVersion7();
        var contactId = Guid.CreateVersion7();
        var request = new CreateActRequest
        {
            CaseId = caseId,
            Direction = ActDirection.Outgoing,
            Date = new DateOnly(2026, 8, 25),
            Title = "Odvolání",
            ContactId = contactId,
        };

        var act = ActWriter.BuildAct(request, "EC/20260821-001/20260825-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.CaseId, Is.EqualTo(caseId));
            Assert.That(act.ActNumber, Is.EqualTo("EC/20260821-001/20260825-001"));
            Assert.That(act.Direction, Is.EqualTo(ActDirection.Outgoing));
            Assert.That(act.Title, Is.EqualTo("Odvolání"));
            Assert.That(act.Date, Is.EqualTo(request.Date));
            Assert.That(act.ContactId, Is.EqualTo(contactId));
        }
    }

    [Test]
    public void ATitleAndDescriptionAreStoredTrimmed()
    {
        var request = Request() with { Title = "  Odvolání  ", Description = "  text  " };

        var act = ActWriter.BuildAct(request, "EC/20260821-001/20260825-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.Title, Is.EqualTo("Odvolání"), "a created act stores its title trimmed");
            Assert.That(act.Description, Is.EqualTo("text"), "a created act stores its description trimmed");
        }
    }

    [Test]
    public void ABlankDescriptionIsFiledAsNothing()
    {
        var blank = Request() with { Description = "   " };
        var withText = blank with { Description = "text" };

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ActWriter.BuildAct(blank, "EC/20260821-001/20260825-001").Description, Is.Null);
            Assert.That(ActWriter.BuildAct(withText, "EC/20260821-001/20260825-001").Description, Is.EqualTo("text"));
        }
    }

    [Test]
    public async Task ANumberTakenWhileTheActIsFiledIsIssuedAgain()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 21));
        const string taken = "EC/20260821-001/20260825-001";
        const string free = "EC/20260821-001/20260825-002";

        await this.Tenant.AddAct(@case, new DateOnly(2026, 8, 25), "Podání", actNumber: taken);

        var writer = new ActWriter(new FixedDbSession(this.Tenant.Context), new QueuedActNumberIssuer([taken, free]), NullLogger<ActWriter>.Instance);

        var request = Request() with { Date = new DateOnly(2026, 8, 25) };

        var result = await writer.CreateAct(request with { CaseId = @case.Id }, CancellationToken.None);

        var acts = await this.Tenant.Context.Acts.OfCase(@case.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.Created));
            Assert.That(result.Act!.ActNumber, Is.EqualTo(free), "the loser of the race files under the next free number");
            Assert.That(acts, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task AnActInACaseHoldingAFreedNumberSkipsTheNumbersStillHeldByTheRenumberedCase()
    {
        var caseDay = new DateOnly(2026, 8, 7);
        var actDay = new DateOnly(2026, 8, 12);
        var session = new FixedDbSession(this.Tenant.Context);

        var renumbered = await this.Tenant.AddCase(caseDay);
        var held = await this.Tenant.AddAct(renumbered, actDay);

        await this.Tenant.Context.Cases
            .Where(@case => @case.Id == renumbered.Id)
            .ExecuteUpdateAsync(static setters => setters.SetProperty(static @case => @case.CaseNumber, "EC/20260806-001"));

        var freed = await new CaseNumberIssuer(session).NextCaseNumber(caseDay, CancellationToken.None);
        var reissued = await this.Tenant.AddCase(caseDay, caseNumber: freed);

        var writer = new ActWriter(session, new ActNumberIssuer(session), NullLogger<ActWriter>.Instance);

        var result = await writer.CreateAct(Request(reissued.Id) with { Date = actDay }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(freed, Is.EqualTo(renumbered.CaseNumber), "the rewrite frees the case number for a new case");
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.Created));
            Assert.That(
                result.Act!.ActNumber,
                Is.EqualTo(ActNumberFormat.Compose(freed, actDay, 2)),
                $"the act of the renumbered case keeps {held.ActNumber}, so the new case takes the next one");
        }
    }

    [Test]
    public async Task AnActInACaseThatIsNotThereIsRefused()
    {
        var writer = new ActWriter(new FixedDbSession(this.Tenant.Context), new QueuedActNumberIssuer([]), NullLogger<ActWriter>.Instance);

        var result = await writer.CreateAct(Request(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.CaseNotFound), "an act only exists inside a case of the tenant");
            Assert.That(result.Act, Is.Null);
        }
    }

    [Test]
    public async Task AnActNamingAContactThatIsNotThereIsRefused()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 21));
        var writer = new ActWriter(new FixedDbSession(this.Tenant.Context), new QueuedActNumberIssuer(["EC/20260821-001/20260825-001"]), NullLogger<ActWriter>.Instance);

        var request = Request() with { Direction = ActDirection.Incoming, ContactId = Guid.CreateVersion7() };

        var result = await writer.CreateAct(request with { CaseId = @case.Id }, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.ContactNotFound), "a contact the tenant does not hold never reaches the row");
    }

    [Test]
    public async Task AnActWithNeitherADirectionNorAContactIsFiled()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 21));
        var writer = new ActWriter(new FixedDbSession(this.Tenant.Context), new QueuedActNumberIssuer(["EC/20260821-001/20260825-001"]), NullLogger<ActWriter>.Instance);

        var result = await writer.CreateAct(Request(@case.Id), CancellationToken.None);

        var reloaded = await this.Tenant.Context.Acts.SingleAsync(act => act.Id == result.Act!.ActId);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.Created));
            Assert.That(reloaded.Direction, Is.Null, "an act records no counterparty until one is known");
            Assert.That(reloaded.ContactId, Is.Null, "an act records no counterparty until one is known");
        }
    }

    [Test]
    public async Task AnActNamingTheContactOfAnotherTenantIsRefused()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 21));

        Guid foreignContactId;
        await using (var other = await TestTenant.Create())
        {
            var foreignContact = await other.AddContact("Cizí kontakt");
            foreignContactId = foreignContact.Id;
        }

        // No number is queued: a contact of another tenant never reaches the insert.
        var writer = new ActWriter(new FixedDbSession(this.Tenant.Context), new QueuedActNumberIssuer([]), NullLogger<ActWriter>.Instance);

        var request = Request() with { Direction = ActDirection.Incoming, ContactId = foreignContactId };

        var result = await writer.CreateAct(request with { CaseId = @case.Id }, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.ContactNotFound), "an act never names a contact of another tenant");
    }

    [Test]
    public async Task AFiledActCarriesTheContactName()
    {
        var @case = await this.Tenant.AddCase(new DateOnly(2026, 8, 21));
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var writer = new ActWriter(new FixedDbSession(this.Tenant.Context), new QueuedActNumberIssuer(["EC/20260821-001/20260825-001"]), NullLogger<ActWriter>.Instance);

        var request = Request() with { Direction = ActDirection.Outgoing, ContactId = contact.Id };

        var result = await writer.CreateAct(request with { CaseId = @case.Id }, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Act!.ContactName, Is.EqualTo("Krajský soud ve Vzorově"));
            Assert.That(result.Act!.ActNumber, Does.StartWith(@case.CaseNumber + "/"));
        }
    }

    private static CreateActRequest Request(Guid? caseId = null)
    {
        return new CreateActRequest { CaseId = caseId ?? Guid.CreateVersion7(), Date = new DateOnly(2026, 8, 25), Title = "Podání" };
    }

    private sealed class QueuedActNumberIssuer(IReadOnlyList<string> actNumbers) : IActNumberIssuer
    {
        private int issued;

        public async Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken token)
        {
            return actNumbers[this.issued++];
        }
    }
}

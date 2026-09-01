using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Numbering;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Acts;

public class ActWriterTests
{
    private TestTenant tenant = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create(asHost: true);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public void ANewActCarriesTheRequestAndTheNumberIssuedToIt()
    {
        var caseId = Guid.CreateVersion7();
        var issuedByContactId = Guid.CreateVersion7();
        var addressedToContactId = Guid.CreateVersion7();
        var request = new CreateActRequest
        {
            Direction = ActDirection.Outgoing,
            Date = new DateOnly(2026, 8, 25),
            Title = "Odvolání",
            IssuedByContactId = issuedByContactId,
            AddressedToContactId = addressedToContactId,
        };

        var act = ActWriter.BuildAct(caseId, request, "EC/20260821-001/20260825-001");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(act.CaseId, Is.EqualTo(caseId));
            Assert.That(act.ActNumber, Is.EqualTo("EC/20260821-001/20260825-001"));
            Assert.That(act.Direction, Is.EqualTo(ActDirection.Outgoing));
            Assert.That(act.Title, Is.EqualTo("Odvolání"));
            Assert.That(act.Date, Is.EqualTo(request.Date));
            Assert.That(act.IssuedByContactId, Is.EqualTo(issuedByContactId));
            Assert.That(act.AddressedToContactId, Is.EqualTo(addressedToContactId));
        }
    }

    [Test]
    public void ATitleAndDescriptionAreStoredTrimmed()
    {
        var request = Request() with { Title = "  Odvolání  ", Description = "  text  " };

        var act = ActWriter.BuildAct(Guid.CreateVersion7(), request, "EC/20260821-001/20260825-001");

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
            Assert.That(ActWriter.BuildAct(Guid.CreateVersion7(), blank, "EC/20260821-001/20260825-001").Description, Is.Null);
            Assert.That(ActWriter.BuildAct(Guid.CreateVersion7(), withText, "EC/20260821-001/20260825-001").Description, Is.EqualTo("text"));
        }
    }

    [Test]
    public async Task ANumberTakenWhileTheActIsFiledIsIssuedAgain()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 21));
        const string taken = "EC/20260821-001/20260825-001";
        const string free = "EC/20260821-001/20260825-002";

        await this.tenant.AddAct(@case, new DateOnly(2026, 8, 25), "Podání", actNumber: taken);

        var writer = new ActWriter(new FixedDbSession(this.tenant.Context), new QueuedActNumberIssuer([taken, free]), new FakeFileBlobStore(), NullLogger<ActWriter>.Instance);

        var request = Request() with { Date = new DateOnly(2026, 8, 25), IssuedByContactId = this.tenant.DefaultContact.Id };

        var result = await writer.CreateAct(@case.Id, request, CancellationToken.None);

        var acts = await this.tenant.Context.Acts.OfCase(@case.Id).ToListAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.Created));
            Assert.That(result.Act!.ActNumber, Is.EqualTo(free), "the loser of the race files under the next free number");
            Assert.That(acts, Has.Count.EqualTo(2));
        }
    }

    [Test]
    public async Task AnActInACaseThatIsNotThereIsRefused()
    {
        var writer = new ActWriter(new FixedDbSession(this.tenant.Context), new QueuedActNumberIssuer([]), new FakeFileBlobStore(), NullLogger<ActWriter>.Instance);

        var result = await writer.CreateAct(Guid.CreateVersion7(), Request(), CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.CaseNotFound), "an act only exists inside a case of the tenant");
            Assert.That(result.Act, Is.Null);
        }
    }

    [Test]
    public async Task AnActNamingAContactThatIsNotThereIsRefused()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 21));
        var writer = new ActWriter(new FixedDbSession(this.tenant.Context), new QueuedActNumberIssuer(["EC/20260821-001/20260825-001"]), new FakeFileBlobStore(), NullLogger<ActWriter>.Instance);

        var result = await writer.CreateAct(@case.Id, Request() with { IssuedByContactId = Guid.CreateVersion7() }, CancellationToken.None);

        Assert.That(result.Outcome, Is.EqualTo(ActCreateOutcome.ContactNotFound), "a sender the tenant does not hold never reaches the row");
    }

    [Test]
    public async Task AnActNamingTheContactOfAnotherTenantIsRefused()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 21));

        Guid foreignContactId;
        await using (var other = await TestTenant.Create())
        {
            var foreignContact = await other.AddContact("Cizí kontakt");
            foreignContactId = foreignContact.Id;
        }

        // No number is queued: a contact of another tenant never reaches the insert.
        var writer = new ActWriter(new FixedDbSession(this.tenant.Context), new QueuedActNumberIssuer([]), new FakeFileBlobStore(), NullLogger<ActWriter>.Instance);

        var asSender = await writer.CreateAct(@case.Id, Request() with { IssuedByContactId = foreignContactId }, CancellationToken.None);
        var asRecipient = await writer.CreateAct(
            @case.Id,
            Request() with { IssuedByContactId = this.tenant.DefaultContact.Id, AddressedToContactId = foreignContactId },
            CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(asSender.Outcome, Is.EqualTo(ActCreateOutcome.ContactNotFound), "an act never names a contact of another tenant");
            Assert.That(asRecipient.Outcome, Is.EqualTo(ActCreateOutcome.ContactNotFound), "an act never names a contact of another tenant");
        }
    }

    [Test]
    public async Task AFiledActCarriesTheContactNames()
    {
        var @case = await this.tenant.AddCase(new DateOnly(2026, 8, 21));
        var addressedTo = await this.tenant.AddContact("Krajský soud ve Vzorově");
        var writer = new ActWriter(new FixedDbSession(this.tenant.Context), new QueuedActNumberIssuer(["EC/20260821-001/20260825-001"]), new FakeFileBlobStore(), NullLogger<ActWriter>.Instance);

        var request = Request() with { IssuedByContactId = this.tenant.DefaultContact.Id, AddressedToContactId = addressedTo.Id };

        var result = await writer.CreateAct(@case.Id, request, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Act!.IssuedByName, Is.EqualTo(this.tenant.DefaultContact.Name));
            Assert.That(result.Act!.AddressedToName, Is.EqualTo("Krajský soud ve Vzorově"));
            Assert.That(result.Act!.ActNumber, Does.StartWith(@case.CaseNumber + "/"));
        }
    }

    private static CreateActRequest Request()
    {
        return new CreateActRequest
        {
            Direction = ActDirection.Incoming,
            Date = new DateOnly(2026, 8, 25),
            Title = "Podání",
            IssuedByContactId = Guid.CreateVersion7(),
        };
    }

    private sealed class QueuedActNumberIssuer(IReadOnlyList<string> actNumbers) : IActNumberIssuer
    {
        private int issued;

        public Task<string> NextActNumber(Case @case, DateOnly date, CancellationToken token)
        {
            return Task.FromResult(actNumbers[this.issued++]);
        }
    }
}

using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Acts;

/// <summary>
/// Adds and removes the reference numbers other authorities gave an act, on the rows a real
/// PostgreSQL returns. Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ExternalActNumberWriterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private ExternalActNumberWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new ExternalActNumberWriter(new FixedDbSession(this.Tenant.Context), NullLogger<ExternalActNumberWriter>.Instance);
    }

    [Test]
    public async Task ANumberIsFiledWithItsValueAndTheContactThatAssignedIt()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            act.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.Added));
            Assert.That(reloaded.Value, Is.EqualTo("1 T 45/2026"));
            Assert.That(reloaded.AssignedByContactId, Is.EqualTo(contact.Id));
        }
    }

    [Test]
    public async Task TheValueIsStoredWithoutItsSurroundingSpace()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        await this.writer.AddExternalActNumber(
            @case.Id,
            act.Id,
            new ExternalNumberRequest { Value = "  1 T 45/2026  ", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var reloaded = await this.Reload(act.Id);

        Assert.That(reloaded.Value, Is.EqualTo("1 T 45/2026"));
    }

    [Test]
    public async Task AValueTheActAlreadyCarriesIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        await this.Tenant.AddExternalActNumber(act, "1 T 45/2026", contact);

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            act.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var count = await this.Tenant.Context.ExternalActNumbers.CountAsync(number => number.ActId == act.Id && number.Value == "1 T 45/2026");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.ValueTaken), "the value is unique per act");
            Assert.That(count, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task TheSameValueOnAnotherActIsAccepted()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var first = await this.Tenant.AddAct(@case, Day, "První");
        var second = await this.Tenant.AddAct(@case, Day, "Druhý");
        await this.Tenant.AddExternalActNumber(first, "1 T 45/2026", contact);

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            second.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = contact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.Added), "uniqueness is per act, not per tenant");
    }

    [Test]
    public async Task AnUnknownActIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            Guid.CreateVersion7(),
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var any = await this.Tenant.Context.ExternalActNumbers.AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.ActNotFound));
            Assert.That(any, Is.False, "nothing is written when the act does not exist");
        }
    }

    [Test]
    public async Task AnActOfAnotherCaseIsNotFound()
    {
        var caseA = await this.Tenant.AddCase(Day, "A");
        var caseB = await this.Tenant.AddCase(Day, "B");
        var act = await this.Tenant.AddAct(caseA, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalActNumber(
            caseB.Id,
            act.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var any = await this.Tenant.Context.ExternalActNumbers.AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.ActNotFound), "an act is only ever written under the case it sits in");
            Assert.That(any, Is.False, "nothing is written when the act does not sit in the given case");
        }
    }

    [Test]
    public async Task AnActOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalActNumber(
            otherCase.Id,
            otherAct.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = contact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.ActNotFound), "the tenant query filter is what turns another tenant's act into nothing");
    }

    [Test]
    public async Task AnUnknownContactIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            act.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = Guid.CreateVersion7() },
            CancellationToken.None);

        var any = await this.Tenant.Context.ExternalActNumbers.AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.UnknownContact));
            Assert.That(any, Is.False, "nothing is written when the contact does not exist");
        }
    }

    [Test]
    public async Task AContactOfAnotherTenantIsRefused()
    {
        await using var other = await TestTenant.Create();
        var otherContact = await other.AddContact("Cizí kontakt");
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            act.Id,
            new ExternalNumberRequest { Value = "1 T 45/2026", AssignedByContactId = otherContact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.UnknownContact), "the tenant query filter is what turns another tenant's contact into nothing");
    }

    [Test]
    public async Task ADeleteRemovesTheNumber()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var number = await this.Tenant.AddExternalActNumber(act, "1 T 45/2026", contact);

        var deleted = await this.writer.DeleteExternalActNumber(@case.Id, act.Id, number.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.ExternalActNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(exists, Is.False);
        }
    }

    [Test]
    public async Task ADeleteAimedAtAnotherActsNumberRemovesNothing()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var actA = await this.Tenant.AddAct(@case, Day, "A");
        var actB = await this.Tenant.AddAct(@case, Day, "B");
        var number = await this.Tenant.AddExternalActNumber(actA, "1 T 45/2026", contact);

        var deleted = await this.writer.DeleteExternalActNumber(@case.Id, actB.Id, number.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.ExternalActNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound));
            Assert.That(exists, Is.True);
        }
    }

    [Test]
    public async Task ADeleteUnderAnotherCaseRemovesNothing()
    {
        var caseA = await this.Tenant.AddCase(Day, "A");
        var caseB = await this.Tenant.AddCase(Day, "B");
        var act = await this.Tenant.AddAct(caseA, Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var number = await this.Tenant.AddExternalActNumber(act, "1 T 45/2026", contact);

        var deleted = await this.writer.DeleteExternalActNumber(caseB.Id, act.Id, number.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.ExternalActNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound));
            Assert.That(exists, Is.True);
        }
    }

    [Test]
    public async Task AnUnknownNumberIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);

        var deleted = await this.writer.DeleteExternalActNumber(@case.Id, act.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound));
    }

    [Test]
    public async Task ANumberOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherContact = await other.AddContact("Krajský soud ve Vzorově");
        var otherCase = await other.AddCase(Day);
        var otherAct = await other.AddAct(otherCase, Day);
        var number = await other.AddExternalActNumber(otherAct, "1 T 45/2026", otherContact);

        var deleted = await this.writer.DeleteExternalActNumber(otherCase.Id, otherAct.Id, number.Id, CancellationToken.None);

        var exists = await other.Context.ExternalActNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound));
            Assert.That(exists, Is.True, "the other tenant still holds it");
        }
    }

    [Test]
    public async Task ANumberAddedAgainAfterItsDeleteComesBack()
    {
        var @case = await this.Tenant.AddCase(Day);
        var act = await this.Tenant.AddAct(@case, Day);
        var contact = await this.Tenant.AddContact("Krajský soud");
        var other = await this.Tenant.AddContact("Městský úřad");
        var request = new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id };

        await this.writer.AddExternalActNumber(@case.Id, act.Id, request, CancellationToken.None);
        var added = await this.Reload(act.Id);
        await this.writer.DeleteExternalActNumber(@case.Id, act.Id, added.Id, CancellationToken.None);

        var outcome = await this.writer.AddExternalActNumber(
            @case.Id,
            act.Id,
            request with { AssignedByContactId = other.Id },
            CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var reloaded = await this.Reload(act.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalActNumberOutcome.Added), "the index still holds the deleted number's value, so adding it again must not read as taken");
            Assert.That(reloaded.Id, Is.EqualTo(added.Id), "the number comes back rather than arriving as a second row");
            Assert.That(reloaded.AssignedByContactId, Is.EqualTo(other.Id), "the number comes back with the contact the second add named");
        }
    }

    private async Task<ExternalActNumber> Reload(Guid actId)
    {
        return await this.Tenant.Context.ExternalActNumbers.SingleAsync(number => number.ActId == actId);
    }
}

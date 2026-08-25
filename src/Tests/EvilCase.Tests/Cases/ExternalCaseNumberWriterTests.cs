using EvilBrains.EvilCase.Api.Contract.Cases;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Adds and removes the marks other authorities gave a case, on the rows a real PostgreSQL returns.
/// Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ExternalCaseNumberWriterTests
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private TestTenant tenant = null!;

    private ExternalCaseNumberWriter writer = null!;

    [SetUp]
    public async Task SetUp()
    {
        this.tenant = await TestTenant.Create(asHost: true);
        this.writer = new ExternalCaseNumberWriter(new FixedDbSession(this.tenant.Context), NullLogger<ExternalCaseNumberWriter>.Instance);
    }

    [TearDown]
    public async Task TearDown()
    {
        await this.tenant.DisposeAsync();
    }

    [Test]
    public async Task AMarkIsFiledWithItsValueAndTheContactThatAssignedIt()
    {
        var @case = await this.tenant.AddCase(Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.Added));
            Assert.That(reloaded.Value, Is.EqualTo("VV41/2025/08464"));
            Assert.That(reloaded.AssignedByContactId, Is.EqualTo(contact.Id));
        }
    }

    [Test]
    public async Task TheValueIsStoredWithoutItsSurroundingSpace()
    {
        var @case = await this.tenant.AddCase(Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");

        await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalCaseNumberRequest { Value = "  VV41/2025/08464  ", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        Assert.That(reloaded.Value, Is.EqualTo("VV41/2025/08464"));
    }

    [Test]
    public async Task AValueTheCaseAlreadyCarriesIsRefused()
    {
        var @case = await this.tenant.AddCase(Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        await this.tenant.AddExternalCaseNumber(@case, "VV41/2025/08464", contact);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var count = await this.tenant.Context.ExternalCaseNumbers.CountAsync(number => number.CaseId == @case.Id && number.Value == "VV41/2025/08464");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.ValueTaken), "the value is unique per case");
            Assert.That(count, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task TheSameValueOnAnotherCaseIsAccepted()
    {
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        var first = await this.tenant.AddCase(Day, "První");
        var second = await this.tenant.AddCase(Day, "Druhý");
        await this.tenant.AddExternalCaseNumber(first, "VV41/2025/08464", contact);

        var outcome = await this.writer.AddExternalCaseNumber(
            second.Id,
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.Added), "uniqueness is per case, not per tenant");
    }

    [Test]
    public async Task AnUnknownCaseIsNotFound()
    {
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalCaseNumber(
            Guid.CreateVersion7(),
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var any = await this.tenant.Context.ExternalCaseNumbers.AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.CaseNotFound));
            Assert.That(any, Is.False, "nothing is written when the case does not exist");
        }
    }

    [Test]
    public async Task ACaseOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherCase = await other.AddCase(Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalCaseNumber(
            otherCase.Id,
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.CaseNotFound), "the tenant query filter is what turns another tenant's case into nothing");
    }

    [Test]
    public async Task AnUnknownContactIsRefused()
    {
        var @case = await this.tenant.AddCase(Day);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = Guid.CreateVersion7() },
            CancellationToken.None);

        var any = await this.tenant.Context.ExternalCaseNumbers.AnyAsync();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.UnknownContact));
            Assert.That(any, Is.False, "nothing is written when the contact does not exist");
        }
    }

    [Test]
    public async Task AContactOfAnotherTenantIsRefused()
    {
        await using var other = await TestTenant.Create();
        var otherContact = await other.AddContact("Cizí kontakt");
        var @case = await this.tenant.AddCase(Day);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalCaseNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = otherContact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.UnknownContact), "the tenant query filter is what turns another tenant's contact into nothing");
    }

    [Test]
    public async Task ADeleteRemovesTheMark()
    {
        var @case = await this.tenant.AddCase(Day);
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        var number = await this.tenant.AddExternalCaseNumber(@case, "VV41/2025/08464", contact);

        var deleted = await this.writer.DeleteExternalCaseNumber(@case.Id, number.Id, CancellationToken.None);

        var exists = await this.tenant.Context.ExternalCaseNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.True);
            Assert.That(exists, Is.False);
        }
    }

    [Test]
    public async Task ADeleteAimedAtAnotherCasesMarkRemovesNothing()
    {
        var contact = await this.tenant.AddContact("Krajský soud ve Vzorově");
        var caseA = await this.tenant.AddCase(Day, "A");
        var caseB = await this.tenant.AddCase(Day, "B");
        var number = await this.tenant.AddExternalCaseNumber(caseA, "VV41/2025/08464", contact);

        var deleted = await this.writer.DeleteExternalCaseNumber(caseB.Id, number.Id, CancellationToken.None);

        var exists = await this.tenant.Context.ExternalCaseNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.False);
            Assert.That(exists, Is.True);
        }
    }

    [Test]
    public async Task AnUnknownMarkIsNotFound()
    {
        var @case = await this.tenant.AddCase(Day);

        var deleted = await this.writer.DeleteExternalCaseNumber(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(deleted, Is.False);
    }

    [Test]
    public async Task AMarkOfAnotherTenantIsNotFound()
    {
        await using var other = await TestTenant.Create();
        var otherContact = await other.AddContact("Krajský soud ve Vzorově");
        var otherCase = await other.AddCase(Day);
        var number = await other.AddExternalCaseNumber(otherCase, "VV41/2025/08464", otherContact);

        var deleted = await this.writer.DeleteExternalCaseNumber(otherCase.Id, number.Id, CancellationToken.None);

        var exists = await other.Context.ExternalCaseNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.False, "the tenant query filter is what turns another tenant's mark into nothing");
            Assert.That(exists, Is.True, "the other tenant still holds it");
        }
    }

    private async Task<ExternalCaseNumber> Reload(Guid caseId)
    {
        return await this.tenant.Context.ExternalCaseNumbers.SingleAsync(number => number.CaseId == caseId);
    }
}

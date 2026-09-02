using EvilBrains.EvilCase.Api.Contract.Numbers;
using EvilBrains.EvilCase.Business.Cases;
using EvilBrains.EvilCase.Business.Entities;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace EvilBrains.EvilCase.Tests.Cases;

/// <summary>
/// Adds and removes the marks other authorities gave a case, on the rows a real PostgreSQL returns.
/// Each test seeds a tenant of its own, so none cleans up after itself.
/// </summary>
public class ExternalCaseNumberWriterTests : TenantFixture
{
    private static readonly DateOnly Day = new(2026, 8, 24);

    private ExternalCaseNumberWriter writer = null!;

    protected override bool AsHost => true;

    [SetUp]
    public void SetUpWriter()
    {
        this.writer = new ExternalCaseNumberWriter(new FixedDbSession(this.Tenant.Context), NullLogger<ExternalCaseNumberWriter>.Instance);
    }

    [Test]
    public async Task AMarkIsFiledWithItsValueAndTheContactThatAssignedIt()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
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
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalNumberRequest { Value = "  VV41/2025/08464  ", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var reloaded = await this.Reload(@case.Id);

        Assert.That(reloaded.Value, Is.EqualTo("VV41/2025/08464"));
    }

    [Test]
    public async Task AValueTheCaseAlreadyCarriesIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        await this.Tenant.AddExternalCaseNumber(@case, "VV41/2025/08464", contact);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var count = await this.Tenant.Context.ExternalCaseNumbers.CountAsync(number => number.CaseId == @case.Id && number.Value == "VV41/2025/08464");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.ValueTaken), "the value is unique per case");
            Assert.That(count, Is.EqualTo(1));
        }
    }

    [Test]
    public async Task TheSameValueOnAnotherCaseIsAccepted()
    {
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var first = await this.Tenant.AddCase(Day, "První");
        var second = await this.Tenant.AddCase(Day, "Druhý");
        await this.Tenant.AddExternalCaseNumber(first, "VV41/2025/08464", contact);

        var outcome = await this.writer.AddExternalCaseNumber(
            second.Id,
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.Added), "uniqueness is per case, not per tenant");
    }

    [Test]
    public async Task AnUnknownCaseIsNotFound()
    {
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalCaseNumber(
            Guid.CreateVersion7(),
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        var any = await this.Tenant.Context.ExternalCaseNumbers.AnyAsync();

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
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");

        var outcome = await this.writer.AddExternalCaseNumber(
            otherCase.Id,
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.CaseNotFound), "the tenant query filter is what turns another tenant's case into nothing");
    }

    [Test]
    public async Task AnUnknownContactIsRefused()
    {
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = Guid.CreateVersion7() },
            CancellationToken.None);

        var any = await this.Tenant.Context.ExternalCaseNumbers.AnyAsync();

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
        var @case = await this.Tenant.AddCase(Day);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = otherContact.Id },
            CancellationToken.None);

        Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.UnknownContact), "the tenant query filter is what turns another tenant's contact into nothing");
    }

    [Test]
    public async Task ADeleteRemovesTheMark()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var number = await this.Tenant.AddExternalCaseNumber(@case, "VV41/2025/08464", contact);

        var deleted = await this.writer.DeleteExternalCaseNumber(@case.Id, number.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.ExternalCaseNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.Deleted));
            Assert.That(exists, Is.False);
        }
    }

    [Test]
    public async Task ADeleteAimedAtAnotherCasesMarkRemovesNothing()
    {
        var contact = await this.Tenant.AddContact("Krajský soud ve Vzorově");
        var caseA = await this.Tenant.AddCase(Day, "A");
        var caseB = await this.Tenant.AddCase(Day, "B");
        var number = await this.Tenant.AddExternalCaseNumber(caseA, "VV41/2025/08464", contact);

        var deleted = await this.writer.DeleteExternalCaseNumber(caseB.Id, number.Id, CancellationToken.None);

        var exists = await this.Tenant.Context.ExternalCaseNumbers.AnyAsync(entity => entity.Id == number.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound));
            Assert.That(exists, Is.True);
        }
    }

    [Test]
    public async Task AnUnknownMarkIsNotFound()
    {
        var @case = await this.Tenant.AddCase(Day);

        var deleted = await this.writer.DeleteExternalCaseNumber(@case.Id, Guid.CreateVersion7(), CancellationToken.None);

        Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound));
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
            Assert.That(deleted, Is.EqualTo(DeleteOutcome.NotFound), "the tenant query filter is what turns another tenant's mark into nothing");
            Assert.That(exists, Is.True, "the other tenant still holds it");
        }
    }

    [Test]
    public async Task AMarkAddedAgainAfterItsDeleteComesBack()
    {
        var @case = await this.Tenant.AddCase(Day);
        var contact = await this.Tenant.AddContact("Krajský soud");
        var other = await this.Tenant.AddContact("Městský úřad");
        var request = new ExternalNumberRequest { Value = "VV41/2025/08464", AssignedByContactId = contact.Id };

        await this.writer.AddExternalCaseNumber(@case.Id, request, CancellationToken.None);
        var added = await this.Reload(@case.Id);
        await this.writer.DeleteExternalCaseNumber(@case.Id, added.Id, CancellationToken.None);

        var outcome = await this.writer.AddExternalCaseNumber(
            @case.Id,
            request with { AssignedByContactId = other.Id },
            CancellationToken.None);

        this.Tenant.Context.ChangeTracker.Clear();

        var reloaded = await this.Reload(@case.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(outcome, Is.EqualTo(ExternalCaseNumberOutcome.Added), "the index still holds the deleted mark's value, so adding it again must not read as taken");
            Assert.That(reloaded.Id, Is.EqualTo(added.Id), "the mark comes back rather than arriving as a second row");
            Assert.That(reloaded.AssignedByContactId, Is.EqualTo(other.Id), "the mark comes back with the contact the second add named");
        }
    }

    private async Task<ExternalCaseNumber> Reload(Guid caseId)
    {
        return await this.Tenant.Context.ExternalCaseNumbers.SingleAsync(number => number.CaseId == caseId);
    }
}

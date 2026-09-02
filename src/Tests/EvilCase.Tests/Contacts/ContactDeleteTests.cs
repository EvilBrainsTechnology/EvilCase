using EvilBrains.EvilCase.Business.Acts;
using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Acts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using EvilBrains.EvilCase.Tests.Data.Interceptors;
using EvilBrains.EvilCase.Tests.Seeding;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The delete against a real PostgreSQL. A stamp breaks no foreign key, so what refuses a contact
/// written into use after the checks is the reference test the stamp itself carries.
/// </summary>
public class ContactDeleteTests
{
    [Test]
    public async Task AReferenceWrittenAfterTheChecksLeavesTheContactInUse()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        Case? seededCase = null;
        Guid contactId = default;

        // The act lands between the delete's checks and the statement that stamps, on another
        // connection: the race, without timing.
        var race = new BeforeNonQueryInterceptor(() => WriteIssuedAct(userContext, seededCase!, contactId));

        await using var context = TestDatabase.CreateMigrated(userContext, race);
        var seeded = await SeedContactAndCase(context, tenantId, userId);

        seededCase = seeded.Case;
        contactId = seeded.Contact.Id;

        var writer = new ContactWriter(new FixedDbSession(context), NullLogger<ContactWriter>.Instance);

        var outcome = await writer.DeleteContact(seeded.Contact.Id, CancellationToken.None);

        Assert.That(
            outcome,
            Is.EqualTo(ContactDeleteOutcome.Referenced),
            "a reference written after the checks leaves the contact in use rather than failing the request");
    }

    [Test]
    public void ADuplicateKeyIsReadAsAUniqueViolation()
    {
        var exception = new DbUpdateException(
            "duplicate key",
            new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        Assert.That(exception.IsUniqueViolation(), Is.True);
    }

    [Test]
    public async Task AContactADeletedActNamesIsStillInUse()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var seeded = await SeedContactAndCase(context, tenantId, userId);
        WriteIssuedAct(userContext, seeded.Case, seeded.Contact.Id);

        var writer = new ContactWriter(new FixedDbSession(context), NullLogger<ContactWriter>.Instance);
        var acts = new ActWriter(new FixedDbSession(context), new FakeActNumberIssuer(), NullLogger<ActWriter>.Instance);

        await acts.DeleteAct(seeded.Case.Id, await ActIdOf(context, seeded.Case.Id), CancellationToken.None);

        var outcome = await writer.DeleteContact(seeded.Contact.Id, CancellationToken.None);

        Assert.That(
            outcome,
            Is.EqualTo(ContactDeleteOutcome.Referenced),
            "a stamped act still names the contact, so taking the contact would leave the act nothing to point at");
    }

    private static async Task<Guid> ActIdOf(ApplicationDbContext context, Guid caseId)
    {
        context.ChangeTracker.Clear();

        return await context.Acts.Where(act => act.CaseId == caseId).Select(static act => act.Id).SingleAsync();
    }

    /// <summary>
    /// The contact the delete aims at, and the case an act can hang under. The user holds a second contact
    /// as its default, so the delete's default-contact check passes.
    /// </summary>
    private static async Task<(Contact Contact, Case Case)> SeedContactAndCase(ApplicationDbContext context, Guid tenantId, Guid userId)
    {
        var account = new Account { Name = "contact delete" };
        var tenant = new Tenant { Id = tenantId, AccountId = account.Id, Name = "tenant" };
        var contact = new Contact { TenantId = tenantId, Kind = ContactKind.Authority, Name = "Krajský soud" };
        var defaultContact = new Contact { TenantId = tenantId, Kind = ContactKind.Person, Name = "default" };

        var user = new User
        {
            Id = userId,
            TenantId = tenantId,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = defaultContact.Id,
        };

        var @case = new Case
        {
            TenantId = tenantId,
            UserId = userId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "Přestupek",
            Status = CaseStatus.Active,
        };

        context.Accounts.Add(account);
        context.Tenants.Add(tenant);
        context.Contacts.AddRange(contact, defaultContact);
        context.Users.Add(user);
        context.Cases.Add(@case);
        await context.SaveChangesAsync();

        // The delete runs on rows the request did not write, so nothing the seed added stays tracked.
        context.ChangeTracker.Clear();

        return (contact, @case);
    }

    private static void WriteIssuedAct(IUserContext userContext, Case @case, Guid contactId)
    {
        using var context = TestDatabase.CreateMigrated(userContext);

        context.Acts.Add(new Act
        {
            TenantId = @case.TenantId,
            UserId = @case.UserId,
            CaseId = @case.Id,
            ActNumber = "EC/20260821-001/01",
            Direction = ActDirection.Incoming,
            Title = "Rozhodnutí",
            Date = @case.Date,
            IssuedByContactId = contactId,
        });

        context.SaveChanges();
    }
}

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
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The delete against a real PostgreSQL: a reference written once the checks have run is refused by the
/// foreign key, which no fake decides.
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

        await using var context = TestDatabase.CreateMigrated(userContext);
        var seeded = await SeedContactAndCase(context, tenantId, userId);

        // The act lands between the delete's checks and its save, on another connection: the race, without timing.
        context.SavingChanges += (_, _) => WriteIssuedAct(userContext, seeded.Case, seeded.Contact.Id);

        var writer = new ContactWriter(new FixedDbSession(context), NullLogger<ContactWriter>.Instance);

        var outcome = await writer.DeleteContact(seeded.Contact.Id, CancellationToken.None);

        Assert.That(
            outcome,
            Is.EqualTo(ContactDeleteOutcome.Referenced),
            "a reference written after the checks leaves the contact in use rather than failing the request");
    }

    [Test]
    public async Task ADeleteRefusedByAForeignKeyIsReadAsOne()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        var userId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, userId);

        await using var context = TestDatabase.CreateMigrated(userContext);
        var seeded = await SeedContactAndCase(context, tenantId, userId);
        WriteIssuedAct(userContext, seeded.Case, seeded.Contact.Id);

        context.Contacts.Remove(seeded.Contact);

        Assert.That(
            async () => await context.SaveChangesAsync(),
            Throws.InstanceOf<DbUpdateException>().With.Matches<DbUpdateException>(exception => exception.IsForeignKeyViolation()),
            "the write PostgreSQL refuses for a foreign key is read as a foreign-key violation");
    }

    [Test]
    public void AUniqueViolationIsNotAForeignKeyViolation()
    {
        var exception = new DbUpdateException(
            "duplicate key",
            new PostgresException("duplicate key", "ERROR", "ERROR", PostgresErrorCodes.UniqueViolation));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(exception.IsForeignKeyViolation(), Is.False, "the two readings of a failed write answer for their own error alone");
            Assert.That(exception.IsUniqueViolation(), Is.True, "the two readings of a failed write answer for their own error alone");
        }
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

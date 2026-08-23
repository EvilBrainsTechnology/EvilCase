using EvilBrains.EvilCase.Business.Contacts;
using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EvilBrains.EvilCase.Tests.Contacts;

/// <summary>
/// The delete against a real PostgreSQL: only the server decides whether a foreign key still holds the row.
/// </summary>
public class ContactDeleteTests
{
    [Test]
    public async Task AReferenceTheChecksCannotSeeIsAnsweredAsInUse()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, Guid.CreateVersion7());

        await using var context = TestDatabase.CreateMigrated(userContext);
        var contact = await Seed(context, tenantId);

        var writer = new ContactWriter(new FixedDbSession(context), TimeProvider.System);

        var outcome = await writer.Delete(contact.Id);

        Assert.That(
            outcome,
            Is.EqualTo(ContactDeleteOutcome.Referenced),
            "a reference the checks did not see leaves the contact in use rather than failing the request");
    }

    [Test]
    public async Task ADeleteRefusedByAForeignKeyIsReadAsOne()
    {
        var userContext = new StubUserContext();
        var tenantId = Guid.CreateVersion7();
        using var entered = userContext.Enter(tenantId, Guid.CreateVersion7());

        await using var context = TestDatabase.CreateMigrated(userContext);
        var contact = await Seed(context, tenantId);

        context.Contacts.Remove(contact);

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
    /// A contact of the caller's tenant that a user of another tenant holds as its default: the tenant's
    /// query filters hide that user, so the delete's checks pass and the foreign key is what refuses.
    /// </summary>
    private static async Task<Contact> Seed(ApplicationDbContext context, Guid tenantId)
    {
        var account = new Account { Name = "contact delete" };
        context.Accounts.Add(account);
        await context.SaveChangesAsync();

        var tenant = new Tenant { Id = tenantId, AccountId = account.Id, Name = "owner" };
        var otherTenant = new Tenant { AccountId = account.Id, Name = "other" };
        context.Tenants.AddRange(tenant, otherTenant);
        await context.SaveChangesAsync();

        var contact = new Contact { TenantId = tenantId, Kind = ContactKind.Authority, Name = "Krajský soud" };
        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        context.Users.Add(new User
        {
            TenantId = otherTenant.Id,
            Email = $"{Guid.CreateVersion7()}@example.com",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = contact.Id,
        });
        await context.SaveChangesAsync();

        return contact;
    }
}

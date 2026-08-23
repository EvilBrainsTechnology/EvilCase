using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Data;

namespace EvilBrains.EvilCase.Tests.Auth;

public class UserStoreTests
{
    private const int MaxFailedAttempts = 5;

    private static readonly DateTime LockoutEnd = new(2026, 8, 1, 12, 15, 0, DateTimeKind.Utc);

    private static readonly DateTime EarlierLockoutEnd = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task AUserAndItsDefaultContactGoInOneWrite()
    {
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);
        var tenantId = Guid.CreateVersion7();

        var contact = new Contact { TenantId = tenantId, Kind = ContactKind.Person, Name = "admin@evilcase.test" };
        var user = new User
        {
            TenantId = tenantId,
            Email = "admin@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.Admin,
            DefaultContactId = contact.Id,
        };

        await new UserStore(new FixedDbSession(context)).Add(user, contact, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Saves, Is.EqualTo(1), "a second write would leave a user whose required default contact is not there yet");
            Assert.That(context.Added<Contact>().Single(), Is.SameAs(contact), "the contact the caller passed is written with the user");
            Assert.That(context.Added<User>().Single(), Is.SameAs(user));
        }
    }

    /// <summary>
    /// Both calls start from the same stored counter, as two concurrent sign-ins do.
    /// </summary>
    [Test]
    public async Task EveryFailureCountsEvenWhenNothingIsReadBetweenThem()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await Seed(context);

        await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);
        var recorded = await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recorded, Is.Not.Null, "the store returns the row it just wrote");
            Assert.That(
                recorded!.FailedLoginAttempts,
                Is.EqualTo(2),
                "the database counts the failures, so a second miss cannot write the first one's number again");
            Assert.That(recorded.LockoutEnd, Is.Null, "a failure below the ceiling locks nothing");
        }
    }

    [Test]
    public async Task ReachingTheCeilingLocksTheAccountAndStartsTheCounterOver()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await Seed(context, MaxFailedAttempts - 1);

        var recorded = await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(recorded!.LockoutEnd, Is.EqualTo(LockoutEnd), "the attempt that reaches the ceiling is the one that locks the account");
            Assert.That(recorded.FailedLoginAttempts, Is.Zero, "the counter starts over with the lockout, or the first miss after it elapsed would lock the account again");
        }
    }

    [Test]
    public async Task AFailureBelowTheCeilingLeavesTheLockoutAlone()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await Seed(context, 0, EarlierLockoutEnd);

        var recorded = await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);

        Assert.That(recorded!.LockoutEnd, Is.EqualTo(EarlierLockoutEnd), "only the attempt that reaches the ceiling moves the lockout");
    }

    private static async Task<User> Seed(ApplicationDbContext context, int failedAttempts = 0, DateTime? lockoutEnd = null)
    {
        var account = new Account { Name = "lockout" };
        var tenant = new Tenant { AccountId = account.Id, Name = "lockout" };
        var contact = new Contact { TenantId = tenant.Id, Kind = ContactKind.Person, Name = "lockout" };
        var user = new User
        {
            TenantId = tenant.Id,
            Email = $"{Guid.CreateVersion7()}@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.Admin,
            DefaultContactId = contact.Id,
            FailedLoginAttempts = failedAttempts,
            LockoutEnd = lockoutEnd,
        };

        context.Accounts.Add(account);
        context.Tenants.Add(tenant);
        context.Contacts.Add(contact);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user;
    }
}

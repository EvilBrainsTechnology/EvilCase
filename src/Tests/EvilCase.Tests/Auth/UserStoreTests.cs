using EvilBrains.EvilCase.Auth;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Data;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Auth;

public class UserStoreTests
{
    private const int MaxFailedAttempts = 5;

    private static readonly DateTime LockoutEnd = new(2026, 8, 1, 12, 15, 0, DateTimeKind.Utc);

    private static readonly DateTime EarlierLockoutEnd = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task AUserIsWrittenInOneSave()
    {
        var userContext = new StubUserContext();
        var context = FakeApplicationDbContext.Create(userContext);

        var user = new User
        {
            TenantId = Guid.CreateVersion7(),
            Email = "admin@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.Admin,
        };

        await new UserStore(new FixedDbSession(context)).AddUser(user, CancellationToken.None);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(context.Saves, Is.EqualTo(1));
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
        var user = await SeedUser(context);

        await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);
        var lockout = await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);
        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == user.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                stored.FailedLoginAttempts,
                Is.EqualTo(2),
                "the database counts the failures, so a second miss cannot write the first one's number again");
            Assert.That(lockout, Is.Null, "a failure below the ceiling locks nothing");
        }
    }

    [Test]
    public async Task ReachingTheCeilingLocksTheAccountAndStartsTheCounterOver()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context, MaxFailedAttempts - 1);

        var lockout = await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);
        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == user.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(lockout, Is.EqualTo(LockoutEnd), "the attempt that reaches the ceiling is the one that locks the account");
            Assert.That(stored.FailedLoginAttempts, Is.Zero, "the counter starts over with the lockout, or the first miss after it elapsed would lock the account again");
        }
    }

    [Test]
    public async Task AFailureBelowTheCeilingLeavesTheLockoutAlone()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context, 0, EarlierLockoutEnd);

        var lockout = await store.RecordFailedLogin(user.Id, MaxFailedAttempts, LockoutEnd, CancellationToken.None);

        Assert.That(lockout, Is.EqualTo(EarlierLockoutEnd), "only the attempt that reaches the ceiling moves the lockout");
    }

    [Test]
    public async Task AFailureAgainstARowThatIsGoneLocksNothing()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));

        var lockout = await store.RecordFailedLogin(Guid.CreateVersion7(), MaxFailedAttempts, LockoutEnd, CancellationToken.None);

        Assert.That(lockout, Is.Null, "a user deleted between the read and the write is not locked out");
    }

    /// <summary>
    /// Sign-in names an e-mail and no tenant, so it must still find the row a tenant query filter would
    /// otherwise hide.
    /// </summary>
    [Test]
    public async Task FindByEmailFindsTheUserWithNoTenantInContext()
    {
        var userContext = new StubUserContext();
        await using var context = TestDatabase.CreateMigrated(userContext);
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context);

        var found = await store.FindByEmail(user.Email, CancellationToken.None);

        Assert.That(found?.Id, Is.EqualTo(user.Id), "sign-in must find the user before a tenant is known");
    }

    /// <summary>
    /// The anonymous refresh endpoint calls this before a tenant is known.
    /// </summary>
    [Test]
    public async Task FindByIdFindsTheUserWithNoTenantInContext()
    {
        var userContext = new StubUserContext();
        await using var context = TestDatabase.CreateMigrated(userContext);
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context);

        var found = await store.FindById(user.Id, CancellationToken.None);

        Assert.That(found?.Id, Is.EqualTo(user.Id), "token refresh must find the user before a tenant is known");
    }

    private static async Task<User> SeedUser(ApplicationDbContext context, int failedAttempts = 0, DateTime? lockoutEnd = null)
    {
        var account = new Account { Name = "lockout" };
        var tenant = new Tenant { AccountId = account.Id, Name = "lockout" };
        var user = new User
        {
            TenantId = tenant.Id,
            Email = $"{Guid.CreateVersion7()}@evilcase.test",
            PasswordHash = "unused",
            Role = UserRole.Admin,
            FailedLoginAttempts = failedAttempts,
            LockoutEnd = lockoutEnd,
        };

        context.Accounts.Add(account);
        context.Tenants.Add(tenant);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        // The store writes through SQL, so a later read must not be answered from the tracked row.
        context.ChangeTracker.Clear();

        return user;
    }
}

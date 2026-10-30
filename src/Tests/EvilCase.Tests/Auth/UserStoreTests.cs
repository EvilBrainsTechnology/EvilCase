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

    private static readonly DateTime Now = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime EarlierLockoutEnd = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Test]
    public async Task EveryAttemptCountsEvenWhenNothingIsReadBetweenThem()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context);

        await store.CountLoginAttempt(user.Id, MaxFailedAttempts, Now, LockoutEnd, CancellationToken.None);
        await store.CountLoginAttempt(user.Id, MaxFailedAttempts, Now, LockoutEnd, CancellationToken.None);
        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == user.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                stored.FailedLoginAttempts,
                Is.EqualTo(2),
                "the database counts the attempts, so a second one cannot write the first one's number again");
            Assert.That(stored.LockoutEnd, Is.Null, "an attempt below the ceiling locks nothing");
        }
    }

    [Test]
    public async Task ReachingTheCeilingLocksTheAccountAndStartsTheCounterOver()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context, MaxFailedAttempts - 1);

        var counted = await store.CountLoginAttempt(user.Id, MaxFailedAttempts, Now, LockoutEnd, CancellationToken.None);
        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == user.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counted, Is.True, "the attempt that reaches the ceiling still gets to verify");
            Assert.That(stored.LockoutEnd, Is.EqualTo(LockoutEnd), "the attempt that reaches the ceiling is the one that locks the account");
            Assert.That(stored.FailedLoginAttempts, Is.Zero, "the counter starts over with the lockout, or the first miss after it elapsed would lock the account again");
        }
    }

    [Test]
    public async Task AnAttemptBelowTheCeilingLeavesAnElapsedLockoutAlone()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context, 0, EarlierLockoutEnd);

        await store.CountLoginAttempt(user.Id, MaxFailedAttempts, Now, LockoutEnd, CancellationToken.None);
        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == user.Id);

        Assert.That(stored.LockoutEnd, Is.EqualTo(EarlierLockoutEnd), "only the attempt that reaches the ceiling moves the lockout");
    }

    [Test]
    public async Task ALockedAccountCountsNoAttempt()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));
        var user = await SeedUser(context, 0, LockoutEnd);

        var counted = await store.CountLoginAttempt(user.Id, MaxFailedAttempts, Now, LockoutEnd.AddMinutes(15), CancellationToken.None);
        var stored = await context.Users.IgnoreQueryFilters().SingleAsync(candidate => candidate.Id == user.Id);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(counted, Is.False, "an attempt against a locked account must not verify the password");
            Assert.That(stored.FailedLoginAttempts, Is.Zero);
            Assert.That(stored.LockoutEnd, Is.EqualTo(LockoutEnd), "a refused attempt does not extend the lockout");
        }
    }

    [Test]
    public async Task AnAttemptAgainstARowThatIsGoneCountsNothing()
    {
        await using var context = TestDatabase.CreateMigrated();
        var store = new UserStore(new FixedDbSession(context));

        var counted = await store.CountLoginAttempt(Guid.CreateVersion7(), MaxFailedAttempts, Now, LockoutEnd, CancellationToken.None);

        Assert.That(counted, Is.False, "a user deleted between the read and the write is not counted");
    }

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

using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Cases;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using EvilBrains.EvilCase.Tests.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EvilBrains.EvilCase.Tests.Data.Interceptors;

public class UserWriteInterceptorTests
{
    private static readonly Guid TenantA = Guid.CreateVersion7();

    private static readonly Guid TenantB = Guid.CreateVersion7();

    private static readonly Guid UserA = Guid.CreateVersion7();

    private static readonly Guid UserB = Guid.CreateVersion7();

    private static readonly DateTime Moment = new(2026, 8, 21, 0, 0, 0, DateTimeKind.Utc);

    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp()
    {
        this.context = new ApplicationDbContextFactory().CreateDbContext([]);
    }

    [TearDown]
    public void TearDown()
    {
        this.context.Dispose();
    }

    [Test]
    public void TheWriteStampsTheTenantOnARowCreatedWithoutOne()
    {
        var contact = new Contact { Kind = ContactKind.Person, Name = "test" };
        this.context.Contacts.Add(contact);

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        SaveChanges(interceptor, this.context);

        Assert.That(this.context.Entry(contact).Property(nameof(Contact.TenantId)).CurrentValue, Is.EqualTo(TenantA), "a new tenant row takes the tenant of the write, so no creation has to set it");
    }

    [Test]
    public void TheWriteStampsTheUserOnARowCreatedWithoutOne()
    {
        var @case = NewCase();
        this.context.Cases.Add(@case);

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        SaveChanges(interceptor, this.context);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(this.context.Entry(@case).Property(nameof(Case.TenantId)).CurrentValue, Is.EqualTo(TenantA), "a new row takes the tenant and the user of the write, so no creation has to set them");
            Assert.That(this.context.Entry(@case).Property(nameof(Case.UserId)).CurrentValue, Is.EqualTo(UserA), "a new row takes the tenant and the user of the write, so no creation has to set them");
        }
    }

    [Test]
    public void TheWriteKeepsATenantSetExplicitlyWhereItMatches()
    {
        var added = NewContact(TenantA);
        var modified = NewContact(TenantA);
        var deleted = NewContact(TenantA);

        this.context.Contacts.AddRange(added, modified, deleted);
        this.context.Entry(modified).State = EntityState.Modified;
        this.context.Entry(deleted).State = EntityState.Deleted;

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(() => SaveChanges(interceptor, this.context), Throws.Nothing, "an explicit tenant that matches the write stands");
        Assert.That(this.context.Entry(added).Property(nameof(Contact.TenantId)).CurrentValue, Is.EqualTo(TenantA));
    }

    [Test]
    public void TheWriteKeepsAUserSetExplicitlyWhereItMatches()
    {
        var added = NewCase(UserA, TenantA);
        var modified = NewCase(UserA, TenantA);
        var deleted = NewCase(UserA, TenantA);

        this.context.Cases.AddRange(added, modified, deleted);
        this.context.Entry(modified).State = EntityState.Modified;
        this.context.Entry(deleted).State = EntityState.Deleted;

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(() => SaveChanges(interceptor, this.context), Throws.Nothing, "an explicit user that matches the write stands");
        Assert.That(this.context.Entry(added).Property(nameof(Case.UserId)).CurrentValue, Is.EqualTo(UserA));
    }

    [Test]
    public void ARowCreatedUnderAnotherTenantNeverReachesTheDatabase()
    {
        this.context.Contacts.Add(NewContact(TenantB));

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(
            () => SaveChanges(interceptor, this.context),
            Throws.InvalidOperationException,
            "a row of another tenant is refused, not silently restamped");
    }

    [Test]
    public void ARowCreatedUnderAnotherUserNeverReachesTheDatabase()
    {
        this.context.Cases.Add(NewCase(UserB, TenantA));

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(
            () => SaveChanges(interceptor, this.context),
            Throws.InvalidOperationException,
            "a row of another user is refused, not silently restamped");
    }

    [Test]
    public void AnotherUsersRowIsRefusedWhenItChanges()
    {
        var @case = NewCase(UserB, TenantA);
        this.context.Cases.Add(@case);
        this.context.Entry(@case).State = EntityState.Modified;

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(
            () => SaveChanges(interceptor, this.context),
            Throws.InvalidOperationException,
            "a row of another user in the tenant is visible but not editable");
    }

    [Test]
    public void AnotherUsersRowIsRefusedWhenItIsDeleted()
    {
        var @case = NewCase(UserB, TenantA);
        this.context.Cases.Add(@case);
        this.context.Entry(@case).State = EntityState.Deleted;

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(
            () => SaveChanges(interceptor, this.context),
            Throws.InvalidOperationException,
            "a row of another user in the tenant is visible but not deletable");
    }

    [Test]
    public void TheWriteStampsTheTenantOnAUserCreatedWithoutOne()
    {
        var user = NewUser();
        this.context.Users.Add(user);

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        SaveChanges(interceptor, this.context);

        Assert.That(this.context.Entry(user).Property(nameof(User.TenantId)).CurrentValue, Is.EqualTo(TenantA), "a user takes the tenant of the write, so the seed does not name it");
    }

    [Test]
    public void AUserCreatedUnderAnotherTenantNeverReachesTheDatabase()
    {
        this.context.Users.Add(NewUser(TenantB));

        var userContext = new StubUserContext();
        using var scope = userContext.Enter(TenantA, UserA);
        var interceptor = new UserWriteInterceptor(userContext);

        Assert.That(
            () => SaveChanges(interceptor, this.context),
            Throws.InvalidOperationException,
            "a user of another tenant is refused, not silently restamped");
    }

    [Test]
    public void AWriteWithoutATenantRowNeedsNoContext()
    {
        this.context.RefreshTokens.Add(new RefreshToken
        {
            UserId = Guid.CreateVersion7(),
            AuthSessionId = Guid.CreateVersion7(),
            TokenHash = "hash",
            Expires = Moment.AddDays(1),
            SessionExpires = Moment.AddDays(30),
        });

        var interceptor = new UserWriteInterceptor(new StubUserContext());

        Assert.That(
            () => SaveChanges(interceptor, this.context),
            Throws.Nothing,
            "signing in writes with no tenant and no user");
    }

    private static void SaveChanges(UserWriteInterceptor interceptor, DbContext dbContext)
    {
        interceptor.SavingChanges(new DbContextEventData(null!, null!, dbContext), default);
    }

    private static User NewUser(in Guid tenantId = default)
    {
        return new()
        {
            TenantId = tenantId,
            Email = "user@evilcase.test",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = Guid.CreateVersion7(),
        };
    }

    private static Contact NewContact(in Guid tenant)
    {
        return new() { TenantId = tenant, Kind = ContactKind.Person, Name = "test" };
    }

    private static Case NewCase(in Guid userId = default, in Guid tenantId = default)
    {
        return new()
        {
            TenantId = tenantId,
            UserId = userId,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "test",
            Status = CaseStatus.Active,
        };
    }
}

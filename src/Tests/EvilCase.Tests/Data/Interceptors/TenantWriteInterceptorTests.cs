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

public class TenantWriteInterceptorTests
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

        var tenantContext = new StubTenantContext();
        using var scope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new StubUserContext { UserId = UserA });

        Save(interceptor, this.context);

        Assert.That(this.context.Entry(contact).Property(nameof(Contact.TenantId)).CurrentValue, Is.EqualTo(TenantA), "a new tenant row takes the tenant of the write, so no creation has to set it");
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

        var tenantContext = new StubTenantContext();
        using var scope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new StubUserContext { UserId = UserA });

        Assert.That(() => Save(interceptor, this.context), Throws.Nothing, "an explicit tenant that matches the write stands");
        Assert.That(this.context.Entry(added).Property(nameof(Contact.TenantId)).CurrentValue, Is.EqualTo(TenantA));
    }

    [Test]
    public void ARowCreatedUnderAnotherTenantNeverReachesTheDatabase()
    {
        this.context.Contacts.Add(NewContact(TenantB));

        var tenantContext = new StubTenantContext();
        using var scope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new StubUserContext { UserId = UserA });

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.InvalidOperationException,
            "a row of another tenant is refused, not silently restamped");
    }

    [Test]
    public void AWriteWithoutATenantRowNeedsNoTenant()
    {
        this.context.Users.Add(new User
        {
            TenantId = TenantA,
            Email = "user@evilcase.test",
            PasswordHash = "hash",
            Role = UserRole.User,
            DefaultContactId = Guid.CreateVersion7(),
        });

        this.context.RefreshTokens.Add(new RefreshToken
        {
            UserId = Guid.CreateVersion7(),
            AuthSessionId = Guid.CreateVersion7(),
            TokenHash = "hash",
            Expires = Moment.AddDays(1),
            SessionExpires = Moment.AddDays(30),
        });

        var interceptor = new TenantWriteInterceptor(new StubTenantContext(), new AnonymousUserContext());

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.Nothing,
            "signing in writes without a tenant or a signed-in user");
    }

    [Test]
    public void TheWriteStampsTheUserOnARowCreatedWithoutOne()
    {
        var @case = NewCase(TenantA, Guid.Empty);
        this.context.Cases.Add(@case);

        var tenantContext = new StubTenantContext();
        using var tenantScope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new StubUserContext { UserId = UserA });

        Save(interceptor, this.context);

        Assert.That(this.context.Entry(@case).Property(nameof(Case.UserId)).CurrentValue, Is.EqualTo(UserA), "a new user-owned row takes the user of the write, so no creation has to set it");
    }

    [Test]
    public void ARowCreatedWithNoSignedInUserNeverReachesTheDatabase()
    {
        this.context.Cases.Add(NewCase(TenantA, Guid.Empty));

        var tenantContext = new StubTenantContext();
        using var tenantScope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new AnonymousUserContext());

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.InvalidOperationException,
            "a user-owned row with nobody signed in is refused rather than written with an empty owner");
    }

    [Test]
    public void ARowCreatedUnderAnotherUserNeverReachesTheDatabase()
    {
        this.context.Cases.Add(NewCase(TenantA, UserB));

        var tenantContext = new StubTenantContext();
        using var tenantScope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new StubUserContext { UserId = UserA });

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.InvalidOperationException,
            "a creation naming another user as the owner is refused, not silently restamped");
    }

    [Test]
    public void ARowUpdatedUnderAnotherUserNeverReachesTheDatabase()
    {
        var @case = NewCase(TenantA, UserB);
        this.context.Cases.Add(@case);
        this.context.Entry(@case).State = EntityState.Modified;

        var tenantContext = new StubTenantContext();
        using var tenantScope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new StubUserContext { UserId = UserA });

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.InvalidOperationException,
            "an update to a row of another user is refused, not silently restamped");
    }

    [Test]
    public void TheStartupSeedWritesTheRowsItOwnsWithNoSignedInUser()
    {
        this.context.Cases.Add(NewCase(TenantA, UserB));

        var tenantContext = new StubTenantContext();
        using var tenantScope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext, new AnonymousUserContext());

        var @case = this.context.ChangeTracker.Entries<Case>().Single().Entity;

        Assert.That(() => Save(interceptor, this.context), Throws.Nothing, "the startup seed owns the rows it writes without a request behind it");
        Assert.That(this.context.Entry(@case).Property(nameof(Case.UserId)).CurrentValue, Is.EqualTo(UserB));
    }

    private static void Save(TenantWriteInterceptor interceptor, DbContext dbContext)
    {
        interceptor.SavingChanges(new DbContextEventData(null!, null!, dbContext), default);
    }

    private static Contact NewContact(in Guid tenant)
    {
        return new() { TenantId = tenant, Kind = ContactKind.Person, Name = "test" };
    }

    private static Case NewCase(in Guid tenant, in Guid user)
    {
        return new()
        {
            TenantId = tenant,
            UserId = user,
            CaseNumber = "EC/20260821-001",
            Date = new DateOnly(2026, 8, 21),
            Title = "test",
            Status = CaseStatus.Active,
        };
    }
}

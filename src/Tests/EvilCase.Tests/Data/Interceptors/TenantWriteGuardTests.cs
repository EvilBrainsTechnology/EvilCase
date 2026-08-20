using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Contacts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Interceptors;

public class TenantWriteGuardTests
{
    private static readonly Guid TenantA = Guid.CreateVersion7();

    private static readonly Guid TenantB = Guid.CreateVersion7();

    private static readonly Guid UserId = Guid.CreateVersion7();

    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void ARowOfTheContextsTenantPassesEveryState()
    {
        var added = NewContact(TenantA);
        var modified = NewContact(TenantA);
        var deleted = NewContact(TenantA);

        this.context.AddRange(added, modified, deleted);
        this.context.Entry(modified).State = EntityState.Modified;
        this.context.Entry(deleted).State = EntityState.Deleted;

        Assert.That(() => TenantWriteGuard.Verify(this.context.ChangeTracker, TenantA), Throws.Nothing);
    }

    [Test]
    public void ARowOfAnotherTenantIsRefused()
    {
        this.context.Add(NewContact(TenantB));

        Assert.That(
            () => TenantWriteGuard.Verify(this.context.ChangeTracker, TenantA),
            Throws.InvalidOperationException,
            "a write across tenants is the leak the filter cannot see");
    }

    [Test]
    public void NoTenantWritesNothing()
    {
        this.context.Add(NewContact(TenantA));

        Assert.That(() => TenantWriteGuard.Verify(this.context.ChangeTracker, tenantId: null), Throws.InvalidOperationException);
    }

    [Test]
    public void AnUntenantedRowIsNotChecked()
    {
        this.context.Add(new User
        {
            TenantId = TenantA,
            Email = "user@evilcase.test",
            PasswordHash = "hash",
            Role = UserRole.User,
        });

        Assert.That(
            () => TenantWriteGuard.Verify(this.context.ChangeTracker, tenantId: null),
            Throws.Nothing,
            "sign-in writes a user before any tenant is known");
    }

    private static Contact NewContact(in Guid tenant) => new() { TenantId = tenant, UserId = UserId, Kind = ContactKind.Person, Name = "test" };
}

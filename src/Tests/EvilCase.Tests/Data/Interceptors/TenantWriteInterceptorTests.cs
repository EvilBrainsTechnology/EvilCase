using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
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
        var interceptor = new TenantWriteInterceptor(tenantContext);

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
        var interceptor = new TenantWriteInterceptor(tenantContext);

        Assert.That(() => Save(interceptor, this.context), Throws.Nothing, "an explicit tenant that matches the write stands");
        Assert.That(this.context.Entry(added).Property(nameof(Contact.TenantId)).CurrentValue, Is.EqualTo(TenantA));
    }

    [Test]
    public void ARowCreatedUnderAnotherTenantNeverReachesTheDatabase()
    {
        this.context.Contacts.Add(NewContact(TenantB));

        var tenantContext = new StubTenantContext();
        using var scope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext);

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

        var interceptor = new TenantWriteInterceptor(new StubTenantContext());

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.Nothing,
            "signing in writes without a tenant");
    }

    private static void Save(TenantWriteInterceptor interceptor, DbContext dbContext)
    {
        interceptor.SavingChanges(new DbContextEventData(null!, null!, dbContext), default);
    }

    private static Contact NewContact(in Guid tenant)
    {
        return new() { TenantId = tenant, Kind = ContactKind.Person, Name = "test" };
    }
}

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

        var tenantContext = new StubTenantContext();
        using var scope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext);

        Assert.That(() => Save(interceptor, this.context), Throws.Nothing);
    }

    [Test]
    public void ARowOfAnotherTenantIsRefused()
    {
        this.context.Add(NewContact(TenantB));

        var tenantContext = new StubTenantContext();
        using var scope = tenantContext.Enter(TenantA);
        var interceptor = new TenantWriteInterceptor(tenantContext);

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.InvalidOperationException,
            "a write must not carry another tenant's rows");
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

        var interceptor = new TenantWriteInterceptor(new StubTenantContext());

        Assert.That(
            () => Save(interceptor, this.context),
            Throws.Nothing,
            "signing in writes before a tenant is known");
    }

    private static void Save(TenantWriteInterceptor interceptor, DbContext dbContext) =>
        interceptor.SavingChanges(new DbContextEventData(null!, null!, dbContext), default);

    private static Contact NewContact(in Guid tenant) => new() { TenantId = tenant, UserId = UserId, Kind = ContactKind.Person, Name = "test" };
}

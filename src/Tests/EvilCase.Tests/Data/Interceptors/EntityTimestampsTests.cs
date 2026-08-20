using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using EvilBrains.EvilCase.Data.Interceptors;
using EvilBrains.EvilCase.Data.Migrations.DbContexts;
using EvilBrains.EvilCase.Domain.Contacts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data.Interceptors;

public class EntityTimestampsTests
{
    private static readonly Guid Tenant = Guid.CreateVersion7();

    private static readonly Guid User = Guid.CreateVersion7();

    private static readonly DateTime Now = new(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc);

    private static readonly DateTime Later = new(2026, 8, 1, 13, 0, 0, DateTimeKind.Utc);

    private ApplicationDbContext context = null!;

    [SetUp]
    public void SetUp() => this.context = new ApplicationDbContextFactory().CreateDbContext([]);

    [TearDown]
    public void TearDown() => this.context.Dispose();

    [Test]
    public void AnInsertGetsItsCreatedStampAndNoChangeStamp()
    {
        var contact = NewContact();
        this.context.Add(contact);

        EntityTimestamps.Apply(this.context.ChangeTracker, Now);

        var entry = this.context.Entry(contact);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Property(nameof(IEntity.Created)).CurrentValue, Is.EqualTo(Now));
            Assert.That(entry.Property(nameof(IEntity.Updated)).CurrentValue, Is.Null);
        }
    }

    [Test]
    public void AChangeGetsTheChangeStampAndKeepsItsCreated()
    {
        var contact = NewContact();
        this.context.Add(contact);

        EntityTimestamps.Apply(this.context.ChangeTracker, Now);

        this.context.Entry(contact).State = EntityState.Modified;

        EntityTimestamps.Apply(this.context.ChangeTracker, Later);

        var entry = this.context.Entry(contact);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(entry.Property(nameof(IEntity.Created)).CurrentValue, Is.EqualTo(Now), "the stamps are the interceptor's alone");
            Assert.That(entry.Property(nameof(IEntity.Updated)).CurrentValue, Is.EqualTo(Later), "the stamps are the interceptor's alone");
        }
    }

    private static Contact NewContact() => new() { TenantId = Tenant, UserId = User, Kind = ContactKind.Person, Name = "test" };
}

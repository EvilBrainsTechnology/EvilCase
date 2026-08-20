using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Tenancy;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// A real <see cref="ApplicationDbContext"/> that never opens a connection: <see cref="SaveChangesAsync"/>
/// leaves every entry tracked instead of sending it to a server, so a test reads what was added straight
/// off the change tracker.
/// </summary>
internal sealed class FakeApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantContext tenantContext)
    : ApplicationDbContext(options, tenantContext)
{
    public static FakeApplicationDbContext Create(ITenantContext tenantContext)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(npgsql => npgsql.UseEvilCaseMigrations());

        return new FakeApplicationDbContext(optionsBuilder.Options, tenantContext);
    }

    public IEnumerable<TEntity> Added<TEntity>() where TEntity : class => this.ChangeTracker.Entries<TEntity>().Select(entry => entry.Entity);

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
}

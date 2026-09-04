using EvilBrains.EvilCase.Data;
using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Tests.Data;

/// <summary>
/// UseNpgsql only builds the model; the overridden save never opens a connection.
/// </summary>
internal sealed class FakeApplicationDbContext(DbContextOptions<ApplicationDbContext> options, IUserContext userContext)
    : ApplicationDbContext(options, userContext)
{
    public static FakeApplicationDbContext Create(IUserContext userContext)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(static npgsql => npgsql.UseEvilCaseMigrations());

        return new FakeApplicationDbContext(optionsBuilder.Options, userContext);
    }

    public IEnumerable<TEntity> Added<TEntity>() where TEntity : class
    {
        return this.ChangeTracker.Entries<TEntity>().Select(static entry => entry.Entity);
    }

    public int Saves { get; private set; }

    public Exception? FailNextSave { get; set; }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        this.Saves++;

        if (this.FailNextSave is not { } failure)
            return 0;

        this.FailNextSave = null;

        throw failure;
    }
}

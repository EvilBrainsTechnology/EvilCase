using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Data.Sessions;

/// <summary>
/// Everything the application does over the database: read, write and the transaction spanning both.
/// </summary>
public interface IApplicationDbSession
{
    public IQueryable<TEntity> Query<TEntity>() where TEntity : class;

    public void Add<TEntity>(TEntity entity) where TEntity : class;

    public Task SaveChanges(CancellationToken cancellationToken);

    /// <summary>
    /// Opened by the layer that owns the DI scope, never by a single write.
    /// </summary>
    public Task<IDbContextTransaction> BeginTransaction(CancellationToken cancellationToken);
}

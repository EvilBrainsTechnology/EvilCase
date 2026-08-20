using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Data.Sessions;

internal sealed class ApplicationDbSession(IApplicationDbContextAccessor accessor) : IApplicationDbSession
{
    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => accessor.Current.Set<TEntity>();

    public void Add<TEntity>(TEntity entity) where TEntity : class => accessor.Current.Add(entity);

    public Task SaveChanges(CancellationToken cancellationToken) => accessor.Current.SaveChangesAsync(cancellationToken);

    public Task<IDbContextTransaction> BeginTransaction(CancellationToken cancellationToken) =>
        accessor.Current.Database.BeginTransactionAsync(cancellationToken);
}

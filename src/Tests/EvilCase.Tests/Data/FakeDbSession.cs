using EvilBrains.EvilCase.Data.Sessions;
using Microsoft.EntityFrameworkCore.Storage;

namespace EvilBrains.EvilCase.Tests.Data;

internal sealed class FakeDbSession : IApplicationDbSession
{
    private readonly List<object> added = [];

    public IReadOnlyList<object> Added => this.added;

    public IQueryable<TEntity> Query<TEntity>() where TEntity : class => this.added.OfType<TEntity>().AsQueryable();

    public void Add<TEntity>(TEntity entity) where TEntity : class => this.added.Add(entity);

    public Task SaveChanges(CancellationToken cancellationToken) => Task.CompletedTask;

    public Task<IDbContextTransaction> BeginTransaction(CancellationToken cancellationToken) =>
        throw new NotSupportedException("The seed's transaction is opened by its caller.");
}

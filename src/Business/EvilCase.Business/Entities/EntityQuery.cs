using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Entities;

public static class EntityQuery
{
    public static IQueryable<TEntity> WithId<TEntity>(this IQueryable<TEntity> entities, Guid id)
        where TEntity : IEntity
    {
        return entities.Where(e => e.Id == id);
    }

    public static async Task<TEntity> GetByIdAsync<TEntity>(this IQueryable<TEntity> entities, Guid id, CancellationToken cancellationToken = default)
        where TEntity : IEntity
    {
        return await entities
            .WithId(id)
            .SingleAsync(cancellationToken);
    }

    public static async Task<TEntity?> GetByIdOrDefaultAsync<TEntity>(this IQueryable<TEntity> entities, Guid id, CancellationToken cancellationToken = default)
        where TEntity : IEntity
    {
        return await entities
            .WithId(id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}

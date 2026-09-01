using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Business.Entities;

internal static class EntityQuery
{
    public static IQueryable<TEntity> WithId<TEntity>(this IQueryable<TEntity> entities, Guid entityId)
        where TEntity : IEntity
    {
        return entities.Where(entity => entity.Id == entityId);
    }

    public static async Task<bool> Exists<TEntity>(this IQueryable<TEntity> entities, Guid entityId, CancellationToken token)
        where TEntity : IEntity
    {
        return await entities.WithId(entityId).AnyAsync(token);
    }

    public static IQueryable<TEntity> TakeAtMost<TEntity>(this IQueryable<TEntity> entities, int? count)
    {
        return count is { } cap ? entities.Take(cap) : entities;
    }
}

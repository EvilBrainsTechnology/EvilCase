using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Entities;

public static class EntityQuery
{
    public static IQueryable<TEntity> WithId<TEntity>(this IQueryable<TEntity> entities, Guid entityId)
        where TEntity : IEntity
    {
        return entities.Where(entity => entity.Id == entityId);
    }

    /// <summary>
    /// Caps how many rows come back; no cap returns the whole list.
    /// </summary>
    public static IQueryable<TEntity> TakeAtMost<TEntity>(this IQueryable<TEntity> entities, int? count)
    {
        return count is { } cap ? entities.Take(cap) : entities;
    }
}

using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Entities;

public static class EntityQuery
{
    public static IQueryable<TEntity> WithId<TEntity>(this IQueryable<TEntity> entities, Guid id)
        where TEntity : IEntity
    {
        return entities.Where(entity => entity.Id == id);
    }
}

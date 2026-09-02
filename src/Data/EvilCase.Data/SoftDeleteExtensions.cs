using EvilBrains.EvilCase.Data.DbContexts;
using EvilBrains.EvilCase.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data;

public static class SoftDeleteExtensions
{
    /// <summary>
    /// Stamps every row the query matches and answers how many there were. The stamp is the
    /// transaction's, so one transaction marks a whole cascade with one moment.
    /// </summary>
    public static async Task<int> ExecuteSoftDelete<TEntity>(this IQueryable<TEntity> entities, CancellationToken token)
        where TEntity : class, ISoftDeleteEntity
    {
        return await entities.ExecuteUpdateAsync(
            static setters => setters.SetProperty(
                static entity => entity.Deleted,
                static _ => (DateTime?)DatabaseFunctions.Now()),
            token);
    }

    /// <summary>
    /// Drops the soft-delete filter and leaves the tenant filter standing. A read reaches for it where
    /// a stamped row still counts: a number the unique index holds, a reference that blocks a delete.
    /// </summary>
    public static IQueryable<TEntity> IncludingDeleted<TEntity>(this IQueryable<TEntity> entities)
        where TEntity : class, ISoftDeleteEntity
    {
        return entities.IgnoreQueryFilters([ApplicationDbContext.SoftDeleteFilter]);
    }

    /// <summary>
    /// The stamped rows alone, which is where a value the unique index still holds comes back from.
    /// </summary>
    public static IQueryable<TEntity> OnlyDeleted<TEntity>(this IQueryable<TEntity> entities)
        where TEntity : class, ISoftDeleteEntity
    {
        return entities
            .IncludingDeleted()
            .Where(static entity => entity.Deleted != null);
    }

    /// <summary>
    /// The rows no stamp has taken, spelled out where the query dropped the filter for its own reason.
    /// </summary>
    public static IQueryable<TEntity> NotDeleted<TEntity>(this IQueryable<TEntity> entities)
        where TEntity : class, ISoftDeleteEntity
    {
        return entities.Where(static entity => entity.Deleted == null);
    }
}

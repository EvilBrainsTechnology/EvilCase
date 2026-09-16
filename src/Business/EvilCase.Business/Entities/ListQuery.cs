using System.Linq.Expressions;
using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Data.Entities;

namespace EvilBrains.EvilCase.Business.Entities;

internal static class ListQuery
{
    public static IOrderedQueryable<TEntity> InKeyOrder<TEntity, TKey>(
        this IQueryable<TEntity> entities, Expression<Func<TEntity, TKey>> key, ListSortDirection direction)
    {
        return direction == ListSortDirection.Ascending ? entities.OrderBy(key) : entities.OrderByDescending(key);
    }

    /// <summary>
    /// The write moment and the identifier behind it make every sort order total, so no row stands
    /// on two pages of the same list.
    /// </summary>
    public static IQueryable<TEntity> ThenInWriteOrder<TEntity>(this IOrderedQueryable<TEntity> entities, ListSortDirection direction)
        where TEntity : IEntity
    {
        return direction == ListSortDirection.Ascending
            ? entities.ThenBy(static entity => entity.Created).ThenBy(static entity => entity.Id)
            : entities.ThenByDescending(static entity => entity.Created).ThenByDescending(static entity => entity.Id);
    }

    public static IQueryable<TEntity> InPage<TEntity>(this IQueryable<TEntity> entities, int skip, int take)
    {
        return entities
            .Skip(skip)
            .Take(take);
    }
}

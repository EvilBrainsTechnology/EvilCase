using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EntityFramework;

public static class AsReadOnlyExtensions
{
    extension<T>(IQueryable<T> collection)
    {
        public async Task<IReadOnlyCollection<T>> AsReadOnlyCollectionAsync(CancellationToken token)
        {
            return await collection.AsReadOnlyListAsync(token);
        }

        public async Task<IReadOnlyList<T>> AsReadOnlyListAsync(CancellationToken token)
        {
            var list = await collection.ToListAsync(token);
            return list.AsReadOnly();
        }

        public async Task<IReadOnlyDictionary<TKey, T>> AsReadOnlyDictionaryAsync<TKey>(Func<T, TKey> keySelector, CancellationToken token)
            where TKey : notnull
        {
            var dict = await collection.ToDictionaryAsync(keySelector, token);
            return dict.AsReadOnly();
        }

        public async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, CancellationToken token)
            where TKey : notnull
        {
            var dict = await collection.ToDictionaryAsync(keySelector, valueSelector, token);
            return dict.AsReadOnly();
        }
    }
}

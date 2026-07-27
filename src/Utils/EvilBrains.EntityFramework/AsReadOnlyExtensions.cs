using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EntityFramework;

#pragma warning disable SA1101 // TODO remove when style cop fixes .NET 10 problems

public static class AsReadOnlyExtensions
{
    extension<T>(IQueryable<T> collection)
    {
        public async Task<IReadOnlyCollection<T>> AsReadOnlyCollectionAsync()
        {
            return await collection.AsReadOnlyListAsync();
        }

        public async Task<IReadOnlyList<T>> AsReadOnlyListAsync()
        {
            var list = await collection.ToListAsync();
            return list.AsReadOnly();
        }

        public async Task<IReadOnlyDictionary<TKey, T>> AsReadOnlyDictionaryAsync<TKey>(Func<T, TKey> keySelector, CancellationToken token = default)
            where TKey : notnull
        {
            var dict = await collection.ToDictionaryAsync(keySelector, token);
            return dict.AsReadOnly();
        }

        public async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TKey, TValue>(Func<T, TKey> keySelector, Func<T, TValue> valueSelector, CancellationToken token = default)
            where TKey : notnull
        {
            var dict = await collection.ToDictionaryAsync(keySelector, valueSelector, token);
            return dict.AsReadOnly();
        }
    }
}

#pragma warning restore SA1101

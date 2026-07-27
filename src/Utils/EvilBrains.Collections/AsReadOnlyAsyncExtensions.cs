namespace EvilBrains.Collections;

public static class AsReadOnlyAsyncExtensions
{
    public static async Task<IReadOnlyCollection<TSource>> AsReadOnlyCollectionAsync<TSource>(this IEnumerable<Task<TSource>> collection)
    {
        var values = await Task.WhenAll(collection);
        return values.AsReadOnlyCollection();
    }

    public static async Task<IReadOnlyDictionary<TKey, TSource>> AsReadOnlyDictionaryAsync<TSource, TKey>(this IEnumerable<Task<TSource>> collection, Func<TSource, TKey> keySelector)
        where TKey : notnull
    {
        var values = await Task.WhenAll(collection);
        return values.AsReadOnlyDictionary(keySelector);
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TSource, TKey, TValue>(this IEnumerable<Task<TSource>> collection, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        where TKey : notnull
    {
        var values = await Task.WhenAll(collection);
        return values.AsReadOnlyDictionary(keySelector, valueSelector);
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TKey, TValue>(this IEnumerable<Task<KeyValuePair<TKey, TValue>>> pairCollection)
        where TKey : notnull
    {
        var values = await Task.WhenAll(pairCollection);
        return values.AsReadOnlyDictionary();
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TKey, TValue>(this IEnumerable<Task<(TKey Key, TValue Value)>> pairs)
        where TKey : notnull
    {
        var values = await Task.WhenAll(pairs);
        return values.AsReadOnlyDictionary();
    }

    public static async Task<IReadOnlyCollection<TSource>> AsReadOnlyCollectionAsync<TSource>(this IAsyncEnumerable<TSource> collection, CancellationToken token = default)
    {
        var array = await collection.ToArrayAsync(token);
        return array.AsReadOnlyCollection();
    }

    public static async Task<IReadOnlyDictionary<TKey, TSource>> AsReadOnlyDictionaryAsync<TSource, TKey>(this IAsyncEnumerable<TSource> collection, Func<TSource, TKey> keySelector, CancellationToken token = default)
        where TKey : notnull
    {
        var array = await collection.ToDictionaryAsync(keySelector, cancellationToken: token);
        return array.AsReadOnlyDictionary();
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TSource, TKey, TValue>(
        this IAsyncEnumerable<TSource> collection,
        Func<TSource, TKey> keySelector,
        Func<TSource, TValue> valueSelector,
        CancellationToken token = default)
        where TKey : notnull
    {
        var array = await collection.ToDictionaryAsync(keySelector, valueSelector, cancellationToken: token);
        return array.AsReadOnlyDictionary();
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TKey, TValue>(this IAsyncEnumerable<KeyValuePair<TKey, TValue>> pairCollection, CancellationToken token = default)
        where TKey : notnull
    {
        var values = await pairCollection.ToArrayAsync(token);
        return values.AsReadOnlyDictionary();
    }

    public static async Task<IReadOnlyDictionary<TKey, TValue>> AsReadOnlyDictionaryAsync<TKey, TValue>(this IAsyncEnumerable<(TKey Key, TValue Value)> pairs, CancellationToken token = default)
        where TKey : notnull
    {
        var values = await pairs.ToArrayAsync(token);
        return values.AsReadOnlyDictionary();
    }
}

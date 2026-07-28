namespace EvilBrains.Collections;

public static class AsReadOnlyExtensions
{
    public static IReadOnlyCollection<T> AsReadOnlyCollection<T>(this IEnumerable<T> collection)
    {
        return collection switch
        {
            IReadOnlyCollection<T> readOnlyCollection => readOnlyCollection,
            _ => collection.AsReadOnlyList(),
        };
    }

    public static IReadOnlyList<T> AsReadOnlyList<T>(this IEnumerable<T> collection)
    {
        return collection switch
        {
            IReadOnlyList<T> readOnlyList => readOnlyList,
            IList<T> list => list.AsReadOnly(),
            _ => collection.ToList().AsReadOnly(),
        };
    }

    public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> pairCollection)
        where TKey : notnull
    {
        return pairCollection switch
        {
            IReadOnlyDictionary<TKey, TValue> readOnlyDictionary => readOnlyDictionary,
            IDictionary<TKey, TValue> dictionary => dictionary.AsReadOnly(),
            _ => new Dictionary<TKey, TValue>(pairCollection).AsReadOnly(),
        };
    }

    public static IReadOnlyDictionary<TKey, TSource> AsReadOnlyDictionary<TSource, TKey>(this IEnumerable<TSource> collection, Func<TSource, TKey> keySelector)
        where TKey : notnull
    {
        return collection.ToDictionary(keySelector, x => x).AsReadOnly();
    }

    public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TSource, TKey, TValue>(this IEnumerable<TSource> collection, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector)
        where TKey : notnull
    {
        return collection.ToDictionary(keySelector, valueSelector).AsReadOnly();
    }

    public static IReadOnlyDictionary<TKey, TValue> AsReadOnlyDictionary<TKey, TValue>(this IEnumerable<(TKey Key, TValue Value)> pairs)
        where TKey : notnull
    {
        return pairs.ToDictionary().AsReadOnly();
    }
}

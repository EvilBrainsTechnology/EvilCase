using System.Collections.ObjectModel;

namespace EvilBrains.Collections.Factories;

#pragma warning disable CA1711 //: Identifiers should not have incorrect suffix
public static class ReadOnlyDictionary
#pragma warning restore CA1711
{
    public static ReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>()
        where TKey : notnull
    {
        return ReadOnlyDictionary<TKey, TValue>.Empty;
    }

    public static ReadOnlyDictionary<TKey, TValue> Single<TKey, TValue>(TKey key, TValue value)
        where TKey : notnull
    {
        return Single(KeyValuePair.Create(key, value));
    }

    public static ReadOnlyDictionary<TKey, TValue> Single<TKey, TValue>((TKey Key, TValue Value) pair)
        where TKey : notnull
    {
        return Single(KeyValuePair.Create(pair.Key, pair.Value));
    }

    public static ReadOnlyDictionary<TKey, TValue> Single<TKey, TValue>(in KeyValuePair<TKey, TValue> pair)
        where TKey : notnull
    {
        return From(pair);
    }

    public static ReadOnlyDictionary<TKey, TValue> From<TKey, TValue>(params IEnumerable<KeyValuePair<TKey, TValue>> elements)
        where TKey : notnull
    {
        return elements.ToDictionary().AsReadOnly();
    }

    public static ReadOnlyDictionary<TKey, TValue> From<TKey, TValue>(params IEnumerable<(TKey Key, TValue Value)> elements)
        where TKey : notnull
    {
        return elements.ToDictionary().AsReadOnly();
    }
}

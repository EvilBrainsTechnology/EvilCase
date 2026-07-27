using EvilBrains.Collections.Factories;

namespace EvilBrains.Collections;

public static class EmptyIfNullExtensions
{
    public static string EmptyIfNull(this string? str) => str ?? "";

    public static IReadOnlyCollection<T> EmptyIfNull<T>(this IReadOnlyCollection<T>? collection) => collection ?? [];

    public static IReadOnlyDictionary<TKey, TValue> EmptyIfNull<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? dictionary)
        where TKey : notnull
    {
        return dictionary ?? ReadOnlyDictionary.Empty<TKey, TValue>();
    }

    public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T>? collection) => collection ?? [];
}

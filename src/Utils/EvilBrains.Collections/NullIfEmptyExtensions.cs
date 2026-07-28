using System.Collections;

namespace EvilBrains.Collections;

public static class NullIfEmptyExtensions
{
    public static string? NullIfEmpty(this string? str)
    {
        return str switch
        {
            null => null,
            "" => null,
            _ => str,
        };
    }

    public static IReadOnlyCollection<T>? NullIfEmpty<T>(this IReadOnlyCollection<T>? collection)
    {
        return collection switch
        {
            null => null,
            { Count: 0 } => null,
            _ => collection,
        };
    }

    public static IReadOnlyDictionary<TKey, TValue>? NullIfEmpty<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue>? dictionary)
    {
        return dictionary switch
        {
            null => null,
            { Count: 0 } => null,
            _ => dictionary,
        };
    }

    public static IEnumerable<T>? NullIfEmpty<T>(this IEnumerable<T>? collection)
    {
        return collection switch
        {
            null => null,
            "" => null,
            string => collection,
            ICollection { Count: 0 } => null,
            ICollection => collection,
            ICollection<T> { Count: 0 } => null,
            ICollection<T> => collection,
            IReadOnlyCollection<T> { Count: 0 } => null,
            IReadOnlyCollection<T> => collection,
            _ => collection.NullIfEmptyInternal(),
        };
    }

    private static IEnumerable<T>? NullIfEmptyInternal<T>(this IEnumerable<T> collection)
    {
        var enumerator = collection.GetEnumerator();

        bool hasValue;

        try
        {
            hasValue = enumerator.MoveNext();
        }
        catch
        {
            enumerator.Dispose();
            throw;
        }

        if (!hasValue)
        {
            enumerator.Dispose();
            return null;
        }

        // Wrapped in a stable enumerable because the open enumerator can be consumed
        // only once — a bare iterator over it would yield garbage on re-enumeration.
        // Trade-off: the source enumerator must stay open for a possible later drain,
        // so abandoning the result after a partial enumeration leaves it undisposed.
        return YieldOpenEnumerator(enumerator).AsStableEnumerable();
    }

    private static IEnumerable<T> YieldOpenEnumerator<T>(IEnumerator<T> enumerator)
    {
        try
        {
            yield return enumerator.Current;
            while (enumerator.MoveNext())
                yield return enumerator.Current;
        }
        finally
        {
            enumerator.Dispose();
        }
    }
}

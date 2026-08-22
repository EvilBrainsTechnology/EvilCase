using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilBrains.Collections.Factories;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public static class ReadOnlyCollection
#pragma warning restore CA1711
{
    public static ReadOnlyCollection<T> Empty<T>()
    {
        return [];
    }

    public static ReadOnlyCollection<T> Single<T>(T element)
    {
        return [element];
    }

    public static ReadOnlyCollection<T> From<T>(params IEnumerable<T> elements)
    {
        return [.. elements];
    }

    public static ReadOnlyCollection<T> Repeat<T>(T element, int count)
    {
        return [.. Enumerable.Repeat(element, count)];
    }

    public static ReadOnlyCollection<T> Range<T>(T start, int count)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count)];
    }

    public static ReadOnlyCollection<T> Range<T>(T start, int count, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count, step)];
    }

    public static ReadOnlyCollection<T> Sequence<T>(T start, T endInclusive)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, T.One)];
    }

    public static ReadOnlyCollection<T> Sequence<T>(T start, T endInclusive, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, step)];
    }
}

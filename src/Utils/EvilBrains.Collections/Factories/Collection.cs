using System.Collections.ObjectModel;
using System.Numerics;

namespace EvilBrains.Collections.Factories;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public static class Collection
#pragma warning restore CA1711
{
    public static Collection<T> Empty<T>()
    {
        return [];
    }

    public static Collection<T> Single<T>(T element)
    {
        return [element];
    }

    public static Collection<T> From<T>(params IEnumerable<T> elements)
    {
        return [.. elements];
    }

    public static Collection<T> Repeat<T>(T element, int count)
    {
        return [.. Enumerable.Repeat(element, count)];
    }

    public static Collection<T> Range<T>(T start, int count)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count)];
    }

    public static Collection<T> Range<T>(T start, int count, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count, step)];
    }

    public static Collection<T> Sequence<T>(T start, T endInclusive)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, T.One)];
    }

    public static Collection<T> Sequence<T>(T start, T endInclusive, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, step)];
    }
}

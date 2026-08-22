using System.Numerics;

namespace EvilBrains.Collections.Factories;

public static class List
{
    public static List<T> Empty<T>()
    {
        return [];
    }

    public static List<T> Single<T>(T element)
    {
        return [element];
    }

    public static List<T> From<T>(params IEnumerable<T> elements)
    {
        return [.. elements];
    }

    public static List<T> Repeat<T>(T element, int count)
    {
        return [.. Enumerable.Repeat(element, count)];
    }

    public static List<T> Range<T>(T start, int count)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count)];
    }

    public static List<T> Range<T>(T start, int count, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count, step)];
    }

    public static List<T> Sequence<T>(T start, T endInclusive)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, T.One)];
    }

    public static List<T> Sequence<T>(T start, T endInclusive, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, step)];
    }
}

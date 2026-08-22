using System.Numerics;

namespace EvilBrains.Collections.Factories;

public static class Array
{
    public static T[] Empty<T>()
    {
        return [];
    }

    public static T[] Single<T>(T element)
    {
        return [element];
    }

    public static T[] From<T>(params IEnumerable<T> elements)
    {
        return [.. elements];
    }

    public static T[] Repeat<T>(T element, int count)
    {
        return [.. Enumerable.Repeat(element, count)];
    }

    public static T[] Range<T>(T start, int count)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count)];
    }

    public static T[] Range<T>(T start, int count, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count, step)];
    }

    public static T[] Sequence<T>(T start, T endInclusive)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, T.One)];
    }

    public static T[] Sequence<T>(T start, T endInclusive, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, step)];
    }
}

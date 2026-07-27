using System.Numerics;

namespace EvilBrains.Collections.Factories;

public static class ReadOnlyList
{
    public static IReadOnlyList<T> Empty<T>() => [];

    public static IReadOnlyList<T> Single<T>(T element) => [element];

    public static IReadOnlyList<T> From<T>(params IEnumerable<T> elements) => [.. elements];

    public static IReadOnlyList<T> Repeat<T>(T element, int count) => [.. Enumerable.Repeat(element, count)];

    public static IReadOnlyList<T> Range<T>(T start, int count)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count)];
    }

    public static IReadOnlyList<T> Range<T>(T start, int count, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Range(start, count, step)];
    }

    public static IReadOnlyList<T> Sequence<T>(T start, T endInclusive)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, T.One)];
    }

    public static IReadOnlyList<T> Sequence<T>(T start, T endInclusive, T step)
        where T : INumber<T>
    {
        return [.. Enumerable.Sequence(start, endInclusive, step)];
    }
}

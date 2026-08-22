using System.Numerics;

namespace EvilBrains.Collections.Factories;

public static class EnumerableExtensions
{
    extension(Enumerable)
    {
        public static IEnumerable<T> Single<T>(T element)
        {
            return [element];
        }

        public static IEnumerable<T> From<T>(params IEnumerable<T> elements)
        {
            return elements;
        }

        public static IEnumerable<T> Range<T>(T start, int count)
            where T : INumber<T>
        {
            return Range(start, count, T.One);
        }

        public static IEnumerable<T> Range<T>(T start, int count, T step)
            where T : INumber<T>
        {
            for (var i = 0; i < count; ++i)
            {
                yield return start;
                start += step;
            }
        }

        public static IEnumerable<T> Sequence<T>(T start, T endInclusive)
            where T : INumber<T>
        {
            return Enumerable.Sequence(start, endInclusive, T.One);
        }

        public static IEnumerable<T> InfiniteSequence<T>(T start)
            where T : INumber<T>
        {
            return Enumerable.InfiniteSequence(start, T.One);
        }
    }
}

namespace EvilBrains.Collections;

public static class StableEnumeratorExtensions
{
    public static IStableEnumerable<T> AsStableEnumerable<T>(this IEnumerable<T> enumerable) => new StableEnumerable<T>(enumerable);
}

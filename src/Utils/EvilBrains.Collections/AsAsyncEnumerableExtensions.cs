namespace EvilBrains.Collections;

public static class AsAsyncEnumerableExtensions
{
    public static async IAsyncEnumerable<T> AsAsyncEnumerable<T>(this IEnumerable<Task<T>> taskCollection)
    {
        foreach (var task in taskCollection)
            yield return await task;
    }
}

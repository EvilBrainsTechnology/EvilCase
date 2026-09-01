namespace EvilBrains.Collections;

public static class WhenAllExtensions
{
    public static async Task WhenAll(this IEnumerable<Task> tasks)
    {
        await Task.WhenAll(tasks);
    }

    public static async Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks)
    {
        return await Task.WhenAll(tasks);
    }
}

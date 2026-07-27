namespace EvilBrains.Collections;

public static class WhenAllExtensions
{
    public static Task WhenAll(this IEnumerable<Task> tasks) => Task.WhenAll(tasks);
}

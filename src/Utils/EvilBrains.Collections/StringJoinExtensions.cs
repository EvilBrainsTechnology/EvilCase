namespace EvilBrains.Collections;

public static class StringJoinExtensions
{
    public static string StringJoin(this IEnumerable<string> source)
    {
        return string.Concat(source);
    }

    public static string StringJoin(this IEnumerable<string> source, char delimiter)
    {
        return string.Join(delimiter, source);
    }

    public static string StringJoin(this IEnumerable<string> source, string delimiter)
    {
        return string.Join(delimiter, source);
    }

    public static string StringJoin<TSource>(this IEnumerable<TSource> source, Func<TSource, string> stringSelector)
    {
        return string.Concat(source.Select(stringSelector));
    }

    public static string StringJoin<TSource>(this IEnumerable<TSource> source, char delimiter, Func<TSource, string> stringSelector)
    {
        return string.Join(delimiter, source.Select(stringSelector));
    }

    public static string StringJoin<TSource>(this IEnumerable<TSource> source, string delimiter, Func<TSource, string> stringSelector)
    {
        return string.Join(delimiter, source.Select(stringSelector));
    }
}

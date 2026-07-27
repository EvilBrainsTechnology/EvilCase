namespace EvilBrains.Collections;

public static class StringJoinExtensions
{
    public static string StringJoin(this IEnumerable<string> source) => string.Concat(source);

    public static string StringJoin(this IEnumerable<string> source, char delimiter) => string.Join(delimiter, source);

    public static string StringJoin(this IEnumerable<string> source, string delimiter) => string.Join(delimiter, source);

    public static string StringJoin<TSource>(this IEnumerable<TSource> source, Func<TSource, string> stringSelector) => string.Concat(source.Select(stringSelector));

    public static string StringJoin<TSource>(this IEnumerable<TSource> source, char delimiter, Func<TSource, string> stringSelector) => string.Join(delimiter, source.Select(stringSelector));

    public static string StringJoin<TSource>(this IEnumerable<TSource> source, string delimiter, Func<TSource, string> stringSelector) => string.Join(delimiter, source.Select(stringSelector));
}

namespace EvilBrains.EvilCase.Data;

public static class LikeExtensions
{
    /// <summary>
    /// The escape character every <c>LIKE</c> in the application passes to the database.
    /// </summary>
    public const string LikeEscape = "\\";

    /// <summary>
    /// The pattern a search term takes: the term itself, anywhere in the column.
    /// </summary>
    public static string ContainsLikePattern(this string value)
    {
        return $"%{value.Trim().EscapeLikeWildcards()}%";
    }

    public static string EscapeLikeWildcards(this string value)
    {
        return value
            .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
            .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
            .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
    }
}

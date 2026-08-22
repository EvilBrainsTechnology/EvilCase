namespace EvilBrains.EvilCase.Data;

/// <summary>
/// Builds the operands of a SQL <c>LIKE</c>.
/// </summary>
public static class LikeExtensions
{
    /// <summary>
    /// The escape character every <c>LIKE</c> in the application passes to the database.
    /// </summary>
    public const string LikeEscape = "\\";

    /// <summary>
    /// Turns a wildcard inside a literal back into the character itself, so only the pattern the caller
    /// adds around it still matches many rows.
    /// </summary>
    public static string EscapeLikeWildcards(this string value) => value
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);
}

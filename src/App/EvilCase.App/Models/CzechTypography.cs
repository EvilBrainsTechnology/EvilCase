using System.Text.RegularExpressions;

namespace EvilBrains.EvilCase.App.Models;

public static partial class CzechTypography
{
    private const string NoBreakSpace = " ";

    /// <summary>
    /// The text with a no-break space where a Czech line may not break: after a one-letter
    /// preposition or conjunction, between a number and its unit, inside a date, between digit
    /// groups, after § and before a dash.
    /// </summary>
    public static string Text(string text)
    {
        return BreakableSpace.Replace(text, NoBreakSpace);
    }

    [GeneratedRegex(@"(?<=(^|[\s(„""])[ksvzouaiKSVZOUAI]) |(?<=§) |(?<=\d\.) (?=\d)|(?<=\d) (?=[\p{L}%])|(?<=\d) (?=\d{3}(?!\d))| (?=[–—])", RegexOptions.ExplicitCapture, matchTimeoutMilliseconds: 1000)]
    private static partial Regex BreakableSpace { get; }
}

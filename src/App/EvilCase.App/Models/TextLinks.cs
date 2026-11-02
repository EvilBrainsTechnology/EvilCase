using System.Text.RegularExpressions;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// Cuts a text into plain stretches and the http and https addresses in it.
/// </summary>
internal static partial class TextLinks
{
    private static readonly char[] Punctuation = ['.', ',', ';', ':', '?', '!'];

    public static IReadOnlyList<LinkSegment> Segments(string text)
    {
        var segments = new List<LinkSegment>();
        var from = 0;

        foreach (Match match in UrlPattern.Matches(text))
        {
            var url = match.Value.TrimEnd(Punctuation);

            while (url.EndsWith(')') && url.AsSpan().Count(')') > url.AsSpan().Count('('))
                url = url[..^1].TrimEnd(Punctuation);

            if (match.Index > from)
                segments.Add(new LinkSegment(text[from..match.Index], Link: false));

            segments.Add(new LinkSegment(url, Link: true));
            from = match.Index + url.Length;
        }

        if (from < text.Length)
            segments.Add(new LinkSegment(text[from..], Link: false));

        return segments;
    }

    [GeneratedRegex("""https?://[^\s"'“”„<>\[\]]+""", RegexOptions.IgnoreCase, matchTimeoutMilliseconds: 1000)]
    private static partial Regex UrlPattern { get; }
}

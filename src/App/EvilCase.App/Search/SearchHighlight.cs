using System.Text;

namespace EvilBrains.EvilCase.App.Search;

/// <summary>
/// Finds the stretches of a text a search term matches, case and diacritics ignored the way the
/// database ignores them.
/// </summary>
internal static class SearchHighlight
{
    private const int SnippetLead = 22;

    private const int SnippetThreshold = 28;

    public static IReadOnlyList<HighlightSegment> Segments(string text, string term)
    {
        var needle = Fold(term.Trim());

        if (needle.Length == 0)
            return [new HighlightSegment(text, Match: false)];

        var haystack = Fold(text);
        var segments = new List<HighlightSegment>();
        var from = 0;
        var index = haystack.IndexOf(needle, StringComparison.Ordinal);

        while (index >= 0)
        {
            if (index > from)
                segments.Add(new HighlightSegment(text[from..index], Match: false));

            segments.Add(new HighlightSegment(text.Substring(index, needle.Length), Match: true));
            from = index + needle.Length;
            index = haystack.IndexOf(needle, from, StringComparison.Ordinal);
        }

        if (from < text.Length)
            segments.Add(new HighlightSegment(text[from..], Match: false));

        return segments;
    }

    /// <summary>
    /// The text on one line, cut in front of the first match when that sits far from the start.
    /// </summary>
    public static string Snippet(string text, string term)
    {
        var line = string.Join(' ', text.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        var index = Fold(line).IndexOf(Fold(term.Trim()), StringComparison.Ordinal);

        return index > SnippetThreshold ? "…" + line[(index - SnippetLead)..] : line;
    }

    /// <summary>
    /// One folded character per character, so an index in the folded text is one in the original.
    /// </summary>
    private static string Fold(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value)
        {
            var folded = character;

            foreach (var part in character.ToString().Normalize(NormalizationForm.FormD))
            {
                if (CharUnicodeInfo.GetUnicodeCategory(part) != UnicodeCategory.NonSpacingMark)
                {
                    folded = part;
                    break;
                }
            }

            builder.Append(char.ToLowerInvariant(folded));
        }

        return builder.ToString();
    }
}

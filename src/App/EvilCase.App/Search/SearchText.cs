using System.Text;

namespace EvilBrains.EvilCase.App.Search;

/// <summary>
/// Narrows a list in the browser the way the database narrows one: case and diacritics ignored.
/// </summary>
internal static class SearchText
{
    public static bool Matches(string text, string search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return true;

        return Fold(text.Normalize()).Contains(Fold(search.Trim().Normalize()), StringComparison.Ordinal);
    }

    /// <summary>
    /// One folded character per character, so an index in the folded text is one in the original.
    /// </summary>
    public static string Fold(string value)
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

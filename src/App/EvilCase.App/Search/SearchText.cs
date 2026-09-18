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

        return Fold(text).Contains(Fold(search), StringComparison.Ordinal);
    }

    private static string Fold(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var character in value.Trim().Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}

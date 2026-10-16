namespace EvilBrains.EvilCase.App.Models;

public static class SearchResultCountDisplay
{
    /// <summary>
    /// "1 výsledek", "3 výsledky", "16 výsledků".
    /// </summary>
    public static string Text(int count)
    {
        var text = count.ToString(CultureInfo.InvariantCulture);

        return count switch
        {
            1 => "1 výsledek",
            2 or 3 or 4 => $"{text} výsledky",
            _ => $"{text} výsledků",
        };
    }
}

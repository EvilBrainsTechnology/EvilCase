namespace EvilBrains.EvilCase.App.Models;

public static class ActCountDisplay
{
    /// <summary>
    /// "1 úkon", "3 úkony", "23 úkonů".
    /// </summary>
    public static string Text(int count)
    {
        var text = count.ToString(CultureInfo.InvariantCulture);

        return count switch
        {
            1 => "1\u00A0úkon",
            2 or 3 or 4 => $"{text}\u00A0úkony",
            _ => $"{text}\u00A0úkonů",
        };
    }

    /// <summary>
    /// The link from an act to its whole case.
    /// </summary>
    public static string AllText(int count)
    {
        var text = count.ToString(CultureInfo.InvariantCulture);

        return count switch
        {
            1 => "Všechny úkony spisu",
            2 or 3 or 4 => $"Všechny {text}\u00A0úkony spisu",
            _ => $"Všech {text}\u00A0úkonů spisu",
        };
    }
}

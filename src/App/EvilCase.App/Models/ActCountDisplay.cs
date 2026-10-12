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
            1 => "1 úkon",
            2 or 3 or 4 => $"{text} úkony",
            _ => $"{text} úkonů",
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
            2 or 3 or 4 => $"Všechny {text} úkony spisu",
            _ => $"Všech {text} úkonů spisu",
        };
    }
}

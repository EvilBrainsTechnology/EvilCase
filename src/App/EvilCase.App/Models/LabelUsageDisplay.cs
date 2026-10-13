namespace EvilBrains.EvilCase.App.Models;

public static class LabelUsageDisplay
{
    /// <summary>
    /// The usage line of a label row: "4 spisy · 1 úkon", or "Nepoužitý" for none.
    /// </summary>
    public static string Line(int caseCount, int actCount)
    {
        if (caseCount == 0 && actCount == 0)
            return "Nepoužitý";

        return string.Join(" · ", Parts(caseCount, actCount));
    }

    /// <summary>
    /// What the delete confirmation says about the label's carriers.
    /// </summary>
    public static string DeleteSentence(int caseCount, int actCount)
    {
        if (caseCount == 0 && actCount == 0)
            return "Štítek nenese žádný spis ani úkon.";

        var carriers = string.Join(" a ", Parts(caseCount, actCount));
        var from = caseCount + actCount == 1 ? "z něj" : "z nich";

        return $"Štítek nese {carriers}. Smazáním {from} zmizí; {Survives(caseCount, actCount)}.";
    }

    private static List<string> Parts(int caseCount, int actCount)
    {
        List<string> parts = [];

        if (caseCount > 0)
            parts.Add($"{caseCount.ToString(CultureInfo.InvariantCulture)} {Plural(caseCount, "spis", "spisy", "spisů")}");

        if (actCount > 0)
            parts.Add($"{actCount.ToString(CultureInfo.InvariantCulture)} {Plural(actCount, "úkon", "úkony", "úkonů")}");

        return parts;
    }

    private static string Survives(int caseCount, int actCount)
    {
        if (caseCount > 0 && actCount > 0)
            return "spisy ani úkony tím nezanikají";

        if (caseCount > 0)
            return caseCount == 1 ? "spis tím nezaniká" : "spisy tím nezanikají";

        return actCount == 1 ? "úkon tím nezaniká" : "úkony tím nezanikají";
    }

    private static string Plural(int count, string one, string few, string many)
    {
        return count switch
        {
            1 => one,
            >= 2 and <= 4 => few,
            _ => many,
        };
    }
}

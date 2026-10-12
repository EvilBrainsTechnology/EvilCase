namespace EvilBrains.EvilCase.App.Models;

public static class ChildCaseCountDisplay
{
    /// <summary>
    /// "6 otevřených · 9 uzavřených", "1 otevřený · 2 uzavřené".
    /// </summary>
    public static string Text(int openCount, int closedCount)
    {
        var openText = openCount.ToString(CultureInfo.InvariantCulture);
        var closedText = closedCount.ToString(CultureInfo.InvariantCulture);

        var open = openCount switch
        {
            1 => "1 otevřený",
            2 or 3 or 4 => $"{openText} otevřené",
            _ => $"{openText} otevřených",
        };

        var closed = closedCount switch
        {
            1 => "1 uzavřený",
            2 or 3 or 4 => $"{closedText} uzavřené",
            _ => $"{closedText} uzavřených",
        };

        return $"{open} · {closed}";
    }
}

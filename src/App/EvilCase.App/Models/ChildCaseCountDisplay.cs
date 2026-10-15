namespace EvilBrains.EvilCase.App.Models;

public static class ChildCaseCountDisplay
{
    /// <summary>
    /// The two parts of "6 otevřených · 9 uzavřených", "1 otevřený · 2 uzavřené".
    /// </summary>
    public static IReadOnlyList<string> Parts(int openCount, int closedCount)
    {
        var openText = openCount.ToString(CultureInfo.InvariantCulture);
        var closedText = closedCount.ToString(CultureInfo.InvariantCulture);

        var open = openCount switch
        {
            1 => "1\u00A0otevřený",
            2 or 3 or 4 => $"{openText}\u00A0otevřené",
            _ => $"{openText}\u00A0otevřených",
        };

        var closed = closedCount switch
        {
            1 => "1\u00A0uzavřený",
            2 or 3 or 4 => $"{closedText}\u00A0uzavřené",
            _ => $"{closedText}\u00A0uzavřených",
        };

        return [open, closed];
    }
}

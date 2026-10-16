namespace EvilBrains.EvilCase.App.Models;

public static class RecordCountDisplay
{
    /// <summary>
    /// "1 záznam", "3 záznamy", "16 záznamů".
    /// </summary>
    public static string Text(int count)
    {
        var text = count.ToString(CultureInfo.InvariantCulture);

        return count switch
        {
            1 => "1 záznam",
            2 or 3 or 4 => $"{text} záznamy",
            _ => $"{text} záznamů",
        };
    }
}

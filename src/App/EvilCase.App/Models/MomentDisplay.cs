namespace EvilBrains.EvilCase.App.Models;

public static class MomentDisplay
{
    private const string Format = "d. M. yyyy H:mm";

    private const string TimeFormat = "H:mm";

    /// <summary>
    /// The moment in the browser's time zone; a dash for none.
    /// </summary>
    public static string Text(DateTime? moment)
    {
        return moment is { } value ? value.ToLocalTime().ToString(Format, CultureInfo.InvariantCulture) : "—";
    }

    /// <summary>
    /// Today's and yesterday's moment as a word and the time; any other in full.
    /// </summary>
    public static string Relative(DateTime moment, DateTime now)
    {
        var local = moment.ToLocalTime();
        var time = local.ToString(TimeFormat, CultureInfo.InvariantCulture);

        return (now.Date - local.Date).Days switch
        {
            0 => $"dnes {time}",
            1 => $"včera {time}",
            _ => Text(moment),
        };
    }
}

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
    /// Today's and yesterday's day as a word; any other as the date.
    /// </summary>
    public static string Day(DateTime moment, DateTime now)
    {
        return (now.Date - moment.ToLocalTime().Date).Days switch
        {
            0 => "dnes",
            1 => "včera",
            _ => DateDisplay.Day(moment),
        };
    }

    public static string Time(DateTime moment)
    {
        return moment.ToLocalTime().ToString(TimeFormat, CultureInfo.InvariantCulture);
    }
}

namespace EvilBrains.EvilCase.App.Models;

public static class MomentDisplay
{
    private const string Format = "d.\u00A0M.\u00A0yyyy H:mm";

    private const string TimeFormat = "H:mm";

    public static string Text(in DateTime moment)
    {
        return moment.ToLocalTime().ToString(Format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Today's and yesterday's moment as a word and the time; any other in full.
    /// </summary>
    public static string Relative(in DateTime moment, in DateTime now)
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

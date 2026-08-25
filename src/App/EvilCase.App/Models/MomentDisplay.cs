namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// Every moment the frontend shows, in one format, in the browser's own time zone.
/// </summary>
public static class MomentDisplay
{
    private const string Format = "d. M. yyyy H:mm";

    public static string Text(in DateTime moment)
    {
        return moment.ToLocalTime().ToString(Format, CultureInfo.InvariantCulture);
    }
}

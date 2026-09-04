namespace EvilBrains.EvilCase.App.Models;

public static class MomentDisplay
{
    private const string Format = "d. M. yyyy H:mm";

    public static string Text(in DateTime moment)
    {
        return moment.ToLocalTime().ToString(Format, CultureInfo.InvariantCulture);
    }
}

namespace EvilBrains.EvilCase.App.Models;

public static class DateDisplay
{
    private const string Format = "d.\u00A0M.\u00A0yyyy";

    public static string Text(in DateOnly date)
    {
        return date.ToString(Format, CultureInfo.InvariantCulture);
    }
}

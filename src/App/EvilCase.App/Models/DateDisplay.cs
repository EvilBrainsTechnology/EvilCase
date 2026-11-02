namespace EvilBrains.EvilCase.App.Models;

public static class DateDisplay
{
    private const string Format = "d. M. yyyy";

    public static string Text(DateOnly date)
    {
        return date.ToString(Format, CultureInfo.InvariantCulture);
    }
}

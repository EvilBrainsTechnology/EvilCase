namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// Every date the frontend shows, in one format.
/// </summary>
public static class DateDisplay
{
    private const string Format = "d. M. yyyy";

    public static string Text(in DateOnly date)
    {
        return date.ToString(Format, CultureInfo.InvariantCulture);
    }
}

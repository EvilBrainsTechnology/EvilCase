namespace EvilBrains.EvilCase.App.Models;

public static class DateDisplay
{
    private const string Format = "d. M. yyyy";

    public static string Text(DateOnly date)
    {
        return date.ToString(Format, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// The day of a moment in the browser's time zone; a dash for none.
    /// </summary>
    public static string Day(DateTime? moment)
    {
        return moment is { } value ? Text(DateOnly.FromDateTime(value.ToLocalTime())) : "—";
    }
}

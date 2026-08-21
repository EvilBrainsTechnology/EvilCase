namespace EvilBrains.EvilCase.App.Models;

public static class CaseListFormat
{
    private const string DateFormat = "d. M. yyyy";

    public static string Date(in DateOnly date) => date.ToString(DateFormat, CultureInfo.InvariantCulture);
}

namespace EvilBrains.EvilCase.App.Models;

public static class CaseDetailFormat
{
    private const string MomentFormat = "d. M. yyyy H:mm";

    public static string Moment(in DateTime moment) =>
        moment.ToLocalTime().ToString(MomentFormat, CultureInfo.InvariantCulture);
}

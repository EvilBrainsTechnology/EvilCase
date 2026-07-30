using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public sealed record DeadlineItem(string CaseNumber, string Title, DateOnly DueOn)
{
    public string DueOnText => this.DueOn.ToString("d. M. yyyy", CultureInfo.InvariantCulture);

    public int DaysLeft => this.DueOn.DayNumber - DateOnly.FromDateTime(DateTime.Today).DayNumber;

    public string DaysLeftText => this.DaysLeft switch
    {
        0 => "dnes",
        1 => "zítra",
        >= 2 and <= 4 => string.Create(CultureInfo.InvariantCulture, $"za {this.DaysLeft} dny"),
        _ => string.Create(CultureInfo.InvariantCulture, $"za {this.DaysLeft} dní"),
    };

    public TablerColor DaysLeftColor => this.DaysLeft switch
    {
        <= 3 => TablerColor.Red,
        <= 7 => TablerColor.Yellow,
        _ => TablerColor.Secondary,
    };
}

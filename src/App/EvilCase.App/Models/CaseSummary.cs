using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public sealed record CaseSummary(string Number, string Title, string Client, CaseStatus Status, DateOnly UpdatedOn)
{
    public string UpdatedOnText => this.UpdatedOn.ToString("d. M. yyyy", CultureInfo.InvariantCulture);

    public string StatusText => this.Status switch
    {
        CaseStatus.Active => "Aktivní",
        CaseStatus.Waiting => "Čeká",
        CaseStatus.Closed => "Uzavřený",
        _ => "",
    };

    public TablerColor StatusColor => this.Status switch
    {
        CaseStatus.Active => TablerColor.Green,
        CaseStatus.Waiting => TablerColor.Yellow,
        CaseStatus.Closed => TablerColor.Secondary,
        _ => TablerColor.Default,
    };
}

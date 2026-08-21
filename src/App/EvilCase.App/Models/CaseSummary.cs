using EvilBrains.EvilCase.Domain.Cases;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public sealed record CaseSummary(string CaseNumber, string Title, string Client, CaseStatus Status, DateOnly UpdatedOn)
{
    public string UpdatedOnText => DateDisplay.Text(this.UpdatedOn);

    public string StatusText => CaseStatusDisplay.Text(this.Status);

    public TablerColor StatusColor => CaseStatusDisplay.Color(this.Status);
}

using EvilBrains.EvilCase.Domain.Cases;
using TabBlazor;

namespace EvilBrains.EvilCase.App.Models;

public sealed record CaseSummary(string Number, string Title, string Client, CaseStatus Status, DateOnly UpdatedOn)
{
    public string UpdatedOnText => this.UpdatedOn.ToString("d. M. yyyy", CultureInfo.InvariantCulture);

    public string StatusText => CaseStatusDisplay.Text(this.Status);

    public TablerColor StatusColor => CaseStatusDisplay.Color(this.Status);
}

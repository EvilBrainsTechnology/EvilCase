using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

public sealed record DashboardView
{
    public required CaseStatusCounts Counts { get; init; }

    public required IReadOnlyList<CaseListItem> ChangedCases { get; init; }

    public required IReadOnlyList<ActListItem> RecentActs { get; init; }

    public bool IsEmpty => this.Counts.Total == 0;
}

using EvilBrains.EvilCase.Api.Contract.Acts;
using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

/// <summary>
/// What the dashboard shows, composed from the entity endpoints it loads (SDD-015).
/// </summary>
public sealed record DashboardView
{
    public required CaseStatusCounts Counts { get; init; }

    public required IReadOnlyList<CaseListItem> ChangedCases { get; init; }

    public required IReadOnlyList<ActListItem> RecentActs { get; init; }

    /// <summary>
    /// A tenant that holds no case at all. A tenant with cases but no act keeps its tiles.
    /// </summary>
    public bool IsEmpty => this.Counts.Total == 0;
}

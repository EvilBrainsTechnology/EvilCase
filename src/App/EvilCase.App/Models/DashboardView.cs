using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.App.Models;

public sealed record DashboardView
{
    public required CaseStatusCounts Counts { get; init; }

    public bool IsEmpty => this.Counts.Total == 0;
}

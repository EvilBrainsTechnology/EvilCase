namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// Whole-tenant counts; the list's search and filter never narrow them.
/// </summary>
public sealed record CaseStatusCounts
{
    public required int Active { get; init; }

    public required int WaitingOnAuthority { get; init; }

    public required int Closed { get; init; }

    public int Total => this.Active + this.WaitingOnAuthority + this.Closed;
}

namespace EvilBrains.EvilCase.Api.Contract.Cases;

/// <summary>
/// How many cases the tenant holds in each status. The whole tenant, whatever the list request
/// narrows to.
/// </summary>
public sealed record CaseStatusCounts
{
    public required int Active { get; init; }

    public required int WaitingOnAuthority { get; init; }

    public required int Closed { get; init; }

    public int Total => this.Active + this.WaitingOnAuthority + this.Closed;
}

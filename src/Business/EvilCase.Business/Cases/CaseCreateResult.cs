using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

/// <summary>
/// What filing a case produced: the case itself only where it was filed.
/// </summary>
public sealed record CaseCreateResult
{
    public required CaseCreateOutcome Outcome { get; init; }

    public CaseListItem? Case { get; init; }
}

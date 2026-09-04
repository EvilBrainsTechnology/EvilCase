using EvilBrains.EvilCase.Api.Contract.Cases;

namespace EvilBrains.EvilCase.Business.Cases;

public sealed record CaseCreateResult
{
    public required CaseCreateOutcome Outcome { get; init; }

    public CaseListItem? Case { get; init; }
}

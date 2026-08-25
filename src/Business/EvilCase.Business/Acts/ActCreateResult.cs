using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

/// <summary>What filing an act produced: the act itself only where it was filed.</summary>
public sealed record ActCreateResult
{
    public required ActCreateOutcome Outcome { get; init; }

    public ActListItem? Act { get; init; }
}

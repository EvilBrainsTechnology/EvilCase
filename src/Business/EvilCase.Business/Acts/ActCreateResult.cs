using EvilBrains.EvilCase.Api.Contract.Acts;

namespace EvilBrains.EvilCase.Business.Acts;

public sealed record ActCreateResult
{
    public required ActCreateOutcome Outcome { get; init; }

    public ActListItem? Act { get; init; }
}

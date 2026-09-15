using EvilBrains.EvilCase.Api.Contract.Labels;

namespace EvilBrains.EvilCase.Business.Labels;

public sealed record LabelCreateResult
{
    public required LabelCreateOutcome Outcome { get; init; }

    public LabelItem? Label { get; init; }
}

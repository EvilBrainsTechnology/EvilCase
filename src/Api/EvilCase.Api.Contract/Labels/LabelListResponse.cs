namespace EvilBrains.EvilCase.Api.Contract.Labels;

public sealed record LabelListResponse
{
    public required IReadOnlyList<LabelItem> Items { get; init; }
}

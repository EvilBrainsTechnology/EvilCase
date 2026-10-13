namespace EvilBrains.EvilCase.Api.Contract.Labels;

public sealed record LabelListResponse
{
    public required IReadOnlyList<LabelListItem> Items { get; init; }
}

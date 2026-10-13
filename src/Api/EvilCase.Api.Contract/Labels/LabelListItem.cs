using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Api.Contract.Labels;

public sealed record LabelListItem
{
    public required Guid LabelId { get; init; }

    public required string Name { get; init; }

    public required LabelColor Color { get; init; }

    public required int CaseCount { get; init; }

    public required int ActCount { get; init; }
}

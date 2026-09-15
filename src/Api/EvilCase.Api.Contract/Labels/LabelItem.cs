using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Api.Contract.Labels;

public sealed record LabelItem
{
    public required Guid LabelId { get; init; }

    public required string Name { get; init; }

    public required LabelColor Color { get; init; }
}

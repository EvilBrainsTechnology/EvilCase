using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Business.Export;

internal sealed record ExportedLabel
{
    public required Guid LabelId { get; init; }

    public required string Name { get; init; }

    public required LabelColor Color { get; init; }
}

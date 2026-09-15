using EvilBrains.EvilCase.Domain.Labels;

namespace EvilBrains.EvilCase.Business.Seeding;

internal sealed record SampleLabel
{
    public required string Key { get; init; }

    public required string Name { get; init; }

    public required LabelColor Color { get; init; }
}

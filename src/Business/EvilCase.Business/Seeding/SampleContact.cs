using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Business.Seeding;

internal sealed record SampleContact
{
    public required string Key { get; init; }

    public required ContactKind Kind { get; init; }

    public required string Name { get; init; }

    public string? Address { get; init; }

    public string? DataBoxId { get; init; }
}

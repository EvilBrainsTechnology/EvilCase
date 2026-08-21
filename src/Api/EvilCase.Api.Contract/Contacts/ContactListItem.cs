using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactListItem
{
    public required Guid Id { get; init; }

    public required ContactKind Kind { get; init; }

    public required string Name { get; init; }

    public string? DataBoxId { get; init; }

    public string? Address { get; init; }
}

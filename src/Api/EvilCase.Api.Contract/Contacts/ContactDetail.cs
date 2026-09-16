using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactDetail
{
    public required Guid ContactId { get; init; }

    public required string Name { get; init; }

    public required ContactKind Kind { get; init; }

    public string? DataBoxId { get; init; }

    public string? Address { get; init; }
}

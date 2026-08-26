using EvilBrains.EvilCase.Domain.Contacts;

namespace EvilBrains.EvilCase.Api.Contract.Contacts;

public sealed record ContactDetail
{
    public required Guid ContactId { get; init; }

    public required string Name { get; init; }

    public required ContactKind Kind { get; init; }

    public string? DataBoxId { get; init; }

    public string? Address { get; init; }

    // Not required: the EF projection sets only the scalar members and the reader fills these with `with`.
    public bool IsDefault { get; init; }

    public IReadOnlyList<ContactCaseOccurrence> Cases { get; init; } = [];

    public IReadOnlyList<ContactActOccurrence> Acts { get; init; } = [];
}

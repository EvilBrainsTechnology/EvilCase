using EvilBrains.EvilCase.Api.Contract.Cases;
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
    public IReadOnlyList<CaseListItem> Cases { get; init; } = [];

    /// <summary>
    /// Only the acts whose contact differs from their case's; the rest are reachable through
    /// <see cref="Cases"/> (SDD-011).
    /// </summary>
    public IReadOnlyList<ContactActOccurrence> Acts { get; init; } = [];
}

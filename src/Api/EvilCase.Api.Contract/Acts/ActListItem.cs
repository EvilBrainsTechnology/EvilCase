using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListItem
{
    public required Guid Id { get; init; }

    public required string ActNumber { get; init; }

    public required ActDirection Direction { get; init; }

    public required string Title { get; init; }

    public required DateOnly Date { get; init; }

    public required string IssuedByName { get; init; }

    public string? AddressedToName { get; init; }
}

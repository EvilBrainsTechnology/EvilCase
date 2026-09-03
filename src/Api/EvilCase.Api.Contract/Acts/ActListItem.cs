using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListItem
{
    public required Guid ActId { get; init; }

    public required Guid CaseId { get; init; }

    public required string CaseNumber { get; init; }

    public required string ActNumber { get; init; }

    public ActDirection? Direction { get; init; }

    public required string Title { get; init; }

    public required DateOnly Date { get; init; }

    public string? ContactName { get; init; }
}

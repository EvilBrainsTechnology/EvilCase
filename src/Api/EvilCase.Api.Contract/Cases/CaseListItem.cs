using EvilBrains.EvilCase.Domain.Cases;

namespace EvilBrains.EvilCase.Api.Contract.Cases;

public sealed record CaseListItem
{
    public required Guid Id { get; init; }

    public required string Title { get; init; }

    public string? Description { get; init; }

    public required CaseStatus Status { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }
}

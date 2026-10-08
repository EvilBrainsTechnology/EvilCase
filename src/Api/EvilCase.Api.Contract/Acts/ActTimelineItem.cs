using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActTimelineItem
{
    public required Guid ActId { get; init; }

    public required string Title { get; init; }

    public required DateOnly Date { get; init; }

    public ActDirection? Direction { get; init; }

    public required bool IsCurrent { get; init; }
}

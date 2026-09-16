using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListRequest : SortableListRequest<ActSortKey>
{
    public override ActSortKey Sort { get; init; } = ActSortKey.Date;

    public string? Search { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public Guid? ContactId { get; init; }

    public IReadOnlyList<Guid> LabelIds { get; init; } = [];

    public Guid? CaseId { get; init; }

    public ActDirection? Direction { get; init; }
}

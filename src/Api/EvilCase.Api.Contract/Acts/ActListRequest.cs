using EvilBrains.EvilCase.Api.Contract.Lists;
using EvilBrains.EvilCase.Domain.Acts;

namespace EvilBrains.EvilCase.Api.Contract.Acts;

public sealed record ActListRequest : ListRequest
{
    public Guid? CaseId { get; init; }

    public ActDirection? Direction { get; init; }

    public ActSortKey Sort { get; init; } = ActSortKey.Date;
}

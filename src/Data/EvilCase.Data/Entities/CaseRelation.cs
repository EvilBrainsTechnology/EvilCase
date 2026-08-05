using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Two cases related to each other, symmetric and stored once with the lower identifier first.
/// The pair is the key, so the row has no identity of its own and is the one entity outside
/// <see cref="IEntity"/>.
/// </summary>
[PrimaryKey(nameof(CaseId), nameof(RelatedCaseId))]
[Index(nameof(RelatedCaseId))]
public record CaseRelation
{
    public required long CaseId { get; init; }

    public required long RelatedCaseId { get; init; }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Two cases of one owner related to each other. The relation is symmetric and stored once, the lower
/// identifier first; neither end is the other's parent. No navigation on either side, so a read names
/// both columns and cannot follow one direction only.
/// </summary>
[Index(nameof(CaseId), nameof(RelatedCaseId), IsUnique = true)]
[Index(nameof(RelatedCaseId))]
public record CaseRelation : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long CaseId { get; init; }

    public required long RelatedCaseId { get; init; }
}

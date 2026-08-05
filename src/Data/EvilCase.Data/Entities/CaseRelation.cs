using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Two cases related to each other, symmetric and stored once with the lower identifier first.
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

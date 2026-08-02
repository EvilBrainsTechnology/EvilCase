using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// One free-text tag on one case. A row rather than an array column so that the set of tags already in
/// use is an indexed query rather than a scan of every case.
/// </summary>
[Index(nameof(CaseId), nameof(Value), IsUnique = true)]
[Index(nameof(Value))]
public record CaseTag : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long CaseId { get; init; }

    /// <summary>
    /// Stored as it was typed.
    /// </summary>
    [MaxLength(64)]
    public required string Value { get; init; }

    public Case? Case { get; init; }
}

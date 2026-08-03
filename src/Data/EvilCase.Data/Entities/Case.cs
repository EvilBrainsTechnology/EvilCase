using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Root of a proceeding. Nesting is the self-reference on <see cref="ParentCaseId"/>: a sub-case has
/// the same shape as what it hangs under, to any depth.
/// </summary>
[Index(nameof(OwnerId))]
[Index(nameof(OwnerId), nameof(InternalCaseReference), IsUnique = true)]
[Index(nameof(ParentCaseId))]
public record Case : IEntity
{
    [Key]
    public long Id { get; init; }

    /// <summary>
    /// Present from this aggregate's first migration, before anything filters on it: until M8 a single
    /// user owns everything, and from M8 on every query and endpoint is scoped by this column.
    /// </summary>
    public required long OwnerId { get; init; }

    /// <summary>
    /// Null on a root case.
    /// </summary>
    public long? ParentCaseId { get; init; }

    [MaxLength(64)]
    public required string InternalCaseReference { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    [MaxLength(4000)]
    public string? Subject { get; init; }

    public required CaseStatus Status { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public User? Owner { get; init; }

    public Case? Parent { get; init; }

    public ICollection<Case> Children { get; init; } = [];

    public ICollection<CaseTag> Tags { get; init; } = [];

    public ICollection<CaseReference> References { get; init; } = [];

    public ICollection<Act> Acts { get; init; } = [];
}

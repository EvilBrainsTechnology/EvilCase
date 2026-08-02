using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Cases;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Root of a proceeding. Nesting is the self-reference on <see cref="ParentCaseId"/>: a sub-case has
/// the same shape as what it hangs under, to any depth.
/// </summary>
[Index(nameof(OwnerId))]
[Index(nameof(OwnerId), nameof(InternalReference), IsUnique = true)]
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

    /// <summary>
    /// The case's own file mark, generated when the case is created and never typed. Exactly one, which
    /// is why it is a column here rather than a row somewhere else: a case without it cannot exist.
    /// Marks assigned by anyone else are <see cref="CaseReference"/> rows.
    /// </summary>
    [MaxLength(64)]
    public required string InternalReference { get; init; }

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
}

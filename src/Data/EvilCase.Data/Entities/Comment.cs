using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A free note on a case or on an act. Exactly one of <see cref="CaseId"/> and <see cref="ActId"/>
/// is set, held by a check constraint.
/// </summary>
[Index(nameof(TenantId))]
[Index(nameof(CaseId))]
[Index(nameof(ActId))]
public record Comment : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public required Guid UserId { get; init; }

    /// <summary>
    /// Set when the note is on a case. Null when it is on an act.
    /// </summary>
    public Guid? CaseId { get; init; }

    /// <summary>
    /// Set when the note is on an act. Null when it is on a case.
    /// </summary>
    public Guid? ActId { get; init; }

    /// <summary>
    /// Unbounded: a note is as long as it needs to be.
    /// </summary>
    public required string Body { get; init; }

    public DateTime Created { get; init; }

    /// <summary>
    /// Set once a note has been edited.
    /// </summary>
    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Act? Act { get; init; }
}

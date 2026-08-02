using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A free note on a case or on an act — the running log of the case.
/// </summary>
/// <remarks>
/// One table rather than two, because the merged timeline (M4) reads every note of a case and all its
/// descendants at once and would otherwise union two shapes. Exactly one of <see cref="CaseId"/> and
/// <see cref="ActId"/> is set, and a check constraint is what holds that rather than a convention
/// nobody can see.
/// </remarks>
[Index(nameof(CaseId))]
[Index(nameof(ActId))]
[Index(nameof(AuthorUserId))]
public record Comment : IEntity
{
    [Key]
    public long Id { get; init; }

    /// <summary>
    /// Set when the note is on a case. Null when it is on an act.
    /// </summary>
    public long? CaseId { get; init; }

    /// <summary>
    /// Set when the note is on an act. Null when it is on a case.
    /// </summary>
    public long? ActId { get; init; }

    /// <summary>
    /// Unbounded: a note is as long as it needs to be.
    /// </summary>
    public required string Body { get; init; }

    public required long AuthorUserId { get; init; }

    public required DateTime Created { get; init; }

    /// <summary>
    /// Set once a note has been edited. Whether the UI offers editing at all is a separate question.
    /// </summary>
    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Act? Act { get; init; }

    public User? Author { get; init; }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A file mark (<em>spisová značka</em>) that somebody else gave this case. Every authority in the chain
/// assigns its own, so a case carries as many of these as there are authorities in it — and none of them
/// is the case's own mark, which is <see cref="Case.InternalReference"/>.
/// </summary>
/// <remarks>
/// This is the mark of the <em>proceeding</em>. The file number (<em>číslo jednací</em>) of a single
/// document belongs to the act that document arrived with, not here.
/// </remarks>
[Index(nameof(CaseId), nameof(Value), IsUnique = true)]
[Index(nameof(Value))]
[Index(nameof(AssignedByPartyId))]
public record CaseReference : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long CaseId { get; init; }

    /// <summary>
    /// The mark as whoever assigned it writes it, spacing and all — it is quoted back to them.
    /// </summary>
    [MaxLength(128)]
    public required string Value { get; init; }

    /// <summary>
    /// Who assigned it. Required: a mark nobody assigned is the case's own, and that one is a column on
    /// the case rather than a row here.
    /// </summary>
    public required long AssignedByPartyId { get; init; }

    public required DateTime Created { get; init; }

    public Case? Case { get; init; }

    public Party? AssignedBy { get; init; }
}

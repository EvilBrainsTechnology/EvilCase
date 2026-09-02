using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A file mark (<em>spisová značka</em>) that somebody else gave this case. Every authority in the chain
/// assigns its own, so a case carries as many of these as there are authorities in it — and none of them
/// is the case's own mark, which is <see cref="Case.CaseNumber"/>.
/// </summary>
[Index(nameof(TenantId), nameof(CaseId), nameof(Value), IsUnique = true)]
[Index(nameof(AssignedByContactId))]
public sealed record ExternalCaseNumber : IUserOwnedEntity, ISoftDeleteEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public required Guid CaseId { get; init; }

    /// <summary>
    /// The mark as whoever assigned it writes it, spacing and all — it is quoted back to them.
    /// </summary>
    [MaxLength(128)]
    public required string Value { get; init; }

    /// <summary>
    /// Who assigned it. Required: a mark nobody assigned is the case's own, and that one is a column on
    /// the case rather than a row here.
    /// </summary>
    public required Guid AssignedByContactId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public DateTime? Deleted { get; init; }

    public Case? Case { get; init; }

    public Contact? AssignedBy { get; init; }
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The reference number (<em>číslo jednací</em>) somebody else gave one act.
/// </summary>
[Index(nameof(TenantId), nameof(ActId), nameof(Value), IsUnique = true)]
[Index(nameof(AssignedByContactId))]
public record ExternalActNumber : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public required Guid ActId { get; init; }

    /// <summary>
    /// The mark as whoever assigned it writes it, spacing and all — it is quoted back to them.
    /// </summary>
    [MaxLength(128)]
    public required string Value { get; init; }

    /// <summary>
    /// Who assigned it. Required: a mark nobody assigned is the act's own, and that one is a column on
    /// the act rather than a row here.
    /// </summary>
    public required Guid AssignedByContactId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Act? Act { get; init; }

    public Contact? AssignedBy { get; init; }
}

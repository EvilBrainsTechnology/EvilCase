using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// A file mark (<em>spisová značka</em>) of one case. Every authority in the chain assigns its own, so a
/// case carries several at once and none of them is the case's identity.
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
    /// The mark as it is written by whoever assigned it, spacing and all — it is quoted back to them.
    /// </summary>
    [MaxLength(128)]
    public required string Value { get; init; }

    /// <summary>
    /// The authority that assigned this mark. Null is what makes a mark the case's own internal one,
    /// and a case has at most one of those — a filtered unique index in
    /// <see cref="DbContexts.ApplicationDbContext"/> is what holds that, rather than a flag that could
    /// disagree with this column.
    /// </summary>
    public long? AssignedByPartyId { get; init; }

    public required DateTime Created { get; init; }

    public Case? Case { get; init; }

    public Party? AssignedBy { get; init; }
}

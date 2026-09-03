using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// The unit of work inside a case, and the thing the user thinks in: one submission, decision, notice
/// or call.
/// </summary>
[Index(nameof(TenantId), nameof(ActNumber), IsUnique = true)]
[Index(nameof(CaseId))]
[Index(nameof(ContactId))]
public sealed record Act : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public required Guid CaseId { get; init; }

    [MaxLength(128)]
    public required string ActNumber { get; init; }

    /// <summary>
    /// The reference number another authority gave this act, as that authority writes it (SDD-010).
    /// </summary>
    [MaxLength(128)]
    public string? ExternalActNumber { get; init; }

    /// <summary>
    /// Which way the act travelled relative to <see cref="Contact"/>. Set exactly when the contact is
    /// (SDD-010); a check constraint holds the pair together.
    /// </summary>
    public ActDirection? Direction { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    /// <summary>
    /// When the act happened, and the only thing act lists sort by. A calendar date, not an instant —
    /// it starts a statutory period (M5) and the hour never enters that arithmetic.
    /// </summary>
    public required DateOnly Date { get; init; }

    public string? Description { get; init; }

    /// <summary>
    /// The counterparty of the act, null where none is recorded (SDD-010).
    /// </summary>
    public Guid? ContactId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Contact? Contact { get; init; }

    public ICollection<Comment> Comments { get; init; } = [];

    public ICollection<FileAsset> Files { get; init; } = [];
}

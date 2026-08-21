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
[Index(nameof(IssuedByContactId))]
[Index(nameof(AddressedToContactId))]
public record Act : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid TenantId { get; init; }

    public required Guid UserId { get; init; }

    public required Guid CaseId { get; init; }

    [MaxLength(128)]
    public required string ActNumber { get; init; }

    public required ActDirection Direction { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    /// <summary>
    /// When the act happened, and the only thing act lists sort by. A calendar date, not an instant —
    /// it starts a statutory period (M5) and the hour never enters that arithmetic.
    /// </summary>
    public required DateOnly Date { get; init; }

    public string? Description { get; init; }

    public required Guid IssuedByContactId { get; init; }

    public Guid? AddressedToContactId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Contact? IssuedByContact { get; init; }

    public Contact? AddressedToContact { get; init; }

    public ICollection<ExternalActNumber> ExternalActNumbers { get; init; } = [];

    public ICollection<Comment> Comments { get; init; } = [];

    public ICollection<FileAsset> Files { get; init; } = [];
}

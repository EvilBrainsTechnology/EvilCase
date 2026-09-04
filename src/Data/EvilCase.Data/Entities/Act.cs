using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Acts;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

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

    [MaxLength(128)]
    public string? ExternalActNumber { get; init; }

    /// <summary>
    /// Null exactly when ContactId is; a check constraint holds the pair.
    /// </summary>
    public ActDirection? Direction { get; init; }

    [MaxLength(256)]
    public required string Title { get; init; }

    public required DateOnly Date { get; init; }

    public string? Description { get; init; }

    public Guid? ContactId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Contact? Contact { get; init; }

    public ICollection<Comment> Comments { get; init; } = [];

    public ICollection<FileAsset> Files { get; init; } = [];
}

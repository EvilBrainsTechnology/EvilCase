using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Stored bytes belonging to exactly one case or one act, XOR held by a check constraint (SDD-012).
/// </summary>
[Index(nameof(TenantId))]
[Index(nameof(CaseId))]
[Index(nameof(ActId))]
public record FileAsset : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? ActId { get; init; }

    /// <summary>
    /// The name it arrived under.
    /// </summary>
    [MaxLength(256)]
    public required string FileName { get; init; }

    /// <summary>
    /// SHA-256 of the content, lower-case hex.
    /// </summary>
    [MaxLength(64)]
    public required string ContentHash { get; init; }

    public required long SizeBytes { get; init; }

    /// <summary>
    /// What the bytes are, where it is known. Never trusted from a file extension.
    /// </summary>
    [MaxLength(128)]
    public string? MediaType { get; init; }

    [MaxLength(256)]
    public required string StoragePath { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Act? Act { get; init; }
}

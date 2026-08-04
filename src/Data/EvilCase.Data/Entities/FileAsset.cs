using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Stored bytes under the act they were filed with, carrying the name they arrived under. Every other
/// act reaches them through an <see cref="ActFileReference"/>.
/// </summary>
[Index(nameof(OwnerId), nameof(ContentHash), IsUnique = true)]
[Index(nameof(OwnerId))]
[Index(nameof(ActId))]
public record FileAsset : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long OwnerId { get; init; }

    public required long ActId { get; init; }

    /// <summary>
    /// The original name, which a reference overrides with its own.
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

    public required DateTime Created { get; init; }

    public User? Owner { get; init; }

    public Act? Act { get; init; }

    public ICollection<ActFileReference> References { get; init; } = [];
}

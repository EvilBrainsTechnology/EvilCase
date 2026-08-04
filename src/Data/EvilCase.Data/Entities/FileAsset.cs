using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Stored bytes, identified by what they are rather than by where they came from. Name and role are
/// not here; they belong to the link.
/// </summary>
[Index(nameof(OwnerId), nameof(ContentHash), IsUnique = true)]
[Index(nameof(OwnerId))]
public record FileAsset : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long OwnerId { get; init; }

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

    public ICollection<ActFileLink> Links { get; init; } = [];
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Stored bytes, identified by what they are rather than by where they came from. The same document
/// cited across six sub-cases is one asset with six links, which is the common case in a real case file
/// and not the rare one.
/// </summary>
/// <remarks>
/// Carries no file name and no role. Both belong to the link: one asset is the final decision under the
/// act that issued it and an attachment under every act that cites it, under a different name each
/// time.
/// </remarks>
[Index(nameof(OwnerId), nameof(ContentHash), IsUnique = true)]
[Index(nameof(OwnerId))]
public record FileAsset : IEntity
{
    [Key]
    public long Id { get; init; }

    /// <summary>
    /// Present from this aggregate's first migration, before anything filters on it. Deduplication is
    /// within one owner and never across owners — sharing a row between two owners would make one
    /// owner's delete another owner's problem, and M8 has enough to enforce already.
    /// </summary>
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

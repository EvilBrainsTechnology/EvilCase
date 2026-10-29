using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// One stored content of a file; the highest Number is the file's current version. The id names the blob.
/// </summary>
[Index(nameof(TenantId), nameof(FileAssetId), nameof(Number), IsUnique = true)]
public sealed record FileVersion : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public required Guid FileAssetId { get; init; }

    public required int Number { get; init; }

    /// <summary>
    /// SHA-256 of the content, lower-case hex.
    /// </summary>
    [MaxLength(64)]
    public required string ContentHash { get; init; }

    public required long SizeBytes { get; init; }

    [MaxLength(128)]
    public string? MediaType { get; init; }

    [MaxLength(256)]
    public required string StoragePath { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public FileAsset? FileAsset { get; init; }
}

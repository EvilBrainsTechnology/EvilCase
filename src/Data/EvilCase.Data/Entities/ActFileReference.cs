using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// One act reaching a file that belongs to another act, under a name of its own.
/// </summary>
[Index(nameof(ActId))]
[Index(nameof(FileAssetId))]
public record ActFileReference : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long ActId { get; init; }

    public required long FileAssetId { get; init; }

    /// <summary>
    /// What the file is called here, overriding the asset's original name.
    /// </summary>
    [MaxLength(256)]
    public required string FileName { get; init; }

    public required DateTime Created { get; init; }

    public Act? Act { get; init; }

    public FileAsset? FileAsset { get; init; }
}

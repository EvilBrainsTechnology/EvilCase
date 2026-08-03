using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.Files;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// One file under one act: which asset, in what role, under what name.
/// </summary>
[Index(nameof(ActId))]
[Index(nameof(FileAssetId))]
[Index(nameof(OriginatingActId))]
public record ActFileLink : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long ActId { get; init; }

    public required long FileAssetId { get; init; }

    public required ActFileRole Role { get; init; }

    /// <summary>
    /// The name this file carries here, which is not a property of the bytes.
    /// </summary>
    [MaxLength(256)]
    public required string FileName { get; init; }

    /// <summary>
    /// Where this asset came from, when it came from another act.
    /// </summary>
    public long? OriginatingActId { get; init; }

    public required DateTime Created { get; init; }

    public Act? Act { get; init; }

    public FileAsset? FileAsset { get; init; }

    public Act? OriginatingAct { get; init; }
}

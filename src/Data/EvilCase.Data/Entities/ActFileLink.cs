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
    /// The name this file carries here, which is not a property of the bytes — the same asset is
    /// "Rozhodnutí" under the act that issued it and "Příloha 2" under the act citing it.
    /// </summary>
    [MaxLength(256)]
    public required string FileName { get; init; }

    /// <summary>
    /// Where this asset came from, when it came from another act. This is what turns an attachment
    /// whose name is a bare date into "the appellate decision of 15 March": the name says nothing and
    /// the originating act says everything.
    /// </summary>
    public long? OriginatingActId { get; init; }

    public required DateTime Created { get; init; }

    public Act? Act { get; init; }

    public FileAsset? FileAsset { get; init; }

    public Act? OriginatingAct { get; init; }
}

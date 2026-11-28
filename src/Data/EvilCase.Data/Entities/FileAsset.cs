using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Its name is unique within its act, ignoring case, through an index the FileVersions migration creates. An act has at
/// most one main file.
/// </summary>
[Index(nameof(TenantId))]
[Index(nameof(ActId))]
public sealed record FileAsset : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public required Guid ActId { get; init; }

    [MaxLength(256)]
    public required string FileName { get; init; }

    public bool IsMain { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Act? Act { get; init; }

    public ICollection<FileVersion> Versions { get; init; } = [];
}

using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Exactly one of CaseId and ActId is set (check constraint). Its name is unique within its case or act, ignoring
/// case, through indexes the FileVersions migration creates.
/// </summary>
[Index(nameof(TenantId))]
[Index(nameof(CaseId))]
[Index(nameof(ActId))]
public sealed record FileAsset : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? ActId { get; init; }

    [MaxLength(256)]
    public required string FileName { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Act? Act { get; init; }

    public ICollection<FileVersion> Versions { get; init; } = [];
}

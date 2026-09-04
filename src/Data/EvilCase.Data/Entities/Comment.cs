using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Exactly one of CaseId and ActId is set (check constraint).
/// </summary>
[Index(nameof(TenantId))]
[Index(nameof(CaseId))]
[Index(nameof(ActId))]
public sealed record Comment : IUserOwnedEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    public Guid UserId { get; init; }

    public Guid? CaseId { get; init; }

    public Guid? ActId { get; init; }

    public required string Body { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public Case? Case { get; init; }

    public Act? Act { get; init; }
}

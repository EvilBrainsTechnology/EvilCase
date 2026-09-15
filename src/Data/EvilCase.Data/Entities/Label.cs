using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Labels;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(TenantId), nameof(Name), IsUnique = true)]
public sealed record Label : ITenantEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    [MaxLength(64)]
    public required string Name { get; init; }

    public required LabelColor Color { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public ICollection<LabelAssignment> Assignments { get; init; } = [];
}

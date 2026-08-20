using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Hranice izolace dat (SDD-006).
/// </summary>
[Index(nameof(AccountId))]
public record Tenant : IEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid AccountId { get; init; }

    [MaxLength(256)]
    public required string Name { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }
}

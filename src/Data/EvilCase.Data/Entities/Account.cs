using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Covers N tenants. Created by the seed at startup and nowhere else (SDD-006).
/// </summary>
public record Account : IEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    [MaxLength(256)]
    public required string Name { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }
}

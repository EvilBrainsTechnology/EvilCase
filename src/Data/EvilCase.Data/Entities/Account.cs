using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// Zastřešuje N tenantů. Vzniká jen seedem při startu (SDD-006).
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

using System.ComponentModel.DataAnnotations;

namespace EvilCase.Data.Entities;

public record TestItem : IEntity
{
    [Key]
    public long Id { get; init; }

    public required DateTime Created { get; init; }

    [MaxLength]
    public required string Text { get; init; }
}

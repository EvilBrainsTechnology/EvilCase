using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(Email), IsUnique = true)]
public record User : IEntity
{
    [Key]
    public long Id { get; init; }

    [MaxLength(128)]
    public required string Email { get; init; }

    [MaxLength(128)]
    public required string PasswordHash { get; init; }

    public required DateTime Created { get; init; }
}

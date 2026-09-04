using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(TenantId))]
public sealed record User : ITenantEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

    /// <summary>
    /// Stored trimmed and lower-cased; the unique index is case-sensitive.
    /// </summary>
    [MaxLength(256)]
    public required string Email { get; init; }

    // The current PBKDF2 encoding is 111 characters; the headroom is for a longer key or another algorithm.
    [MaxLength(256)]
    public required string PasswordHash { get; init; }

    public required UserRole Role { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public int FailedLoginAttempts { get; init; }

    public DateTime? LockoutEnd { get; init; }
}

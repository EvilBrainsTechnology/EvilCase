using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Api.Contract.User;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(Email), IsUnique = true)]
public record User : IEntity
{
    [Key]
    public long Id { get; init; }

    /// <summary>
    /// Stored normalised — trimmed and lower-cased — so the unique index is what makes e-mails
    /// case-insensitive and a lookup never has to fold case in the database.
    /// </summary>
    [MaxLength(256)]
    public required string Email { get; init; }

    // The current PBKDF2 encoding is 111 characters; the headroom is for a longer key or another algorithm.
    [MaxLength(256)]
    public required string PasswordHash { get; init; }

    public required UserRole Role { get; init; }

    public required DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    /// <summary>
    /// Consecutive failed sign-ins. A successful one puts it back to zero.
    /// </summary>
    public int FailedLoginAttempts { get; init; }

    /// <summary>
    /// Set while the account is locked out; in the past means the lockout has elapsed.
    /// </summary>
    public DateTime? LockoutEnd { get; init; }
}

using System.ComponentModel.DataAnnotations;
using EvilBrains.EvilCase.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(Email), IsUnique = true)]
[Index(nameof(TenantId))]
[Index(nameof(DefaultContactId))]
public sealed record User : ITenantEntity, ISoftDeleteEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public Guid TenantId { get; init; }

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

    /// <summary>
    /// The contact the user prefills an act with. It is created with the user, in the same write.
    /// </summary>
    public required Guid DefaultContactId { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public DateTime? Deleted { get; init; }

    /// <summary>
    /// Consecutive failed sign-ins. A successful one puts it back to zero.
    /// </summary>
    public int FailedLoginAttempts { get; init; }

    /// <summary>
    /// Set while the account is locked out; in the past means the lockout has elapsed.
    /// </summary>
    public DateTime? LockoutEnd { get; init; }

    public Contact? DefaultContact { get; init; }
}

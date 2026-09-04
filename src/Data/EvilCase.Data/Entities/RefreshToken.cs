using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(AuthSessionId))]
public sealed record RefreshToken : IEntity
{
    [Key]
    public Guid Id { get; init; } = Guid.CreateVersion7();

    public required Guid UserId { get; init; }

    /// <summary>
    /// Shared by every token of one rotation chain; revocation works on it.
    /// </summary>
    public required Guid AuthSessionId { get; init; }

    /// <summary>
    /// SHA-256 of the token, hex.
    /// </summary>
    [MaxLength(64)]
    public required string TokenHash { get; init; }

    public DateTime Created { get; init; }

    public DateTime? Updated { get; init; }

    public required DateTime Expires { get; init; }

    /// <summary>
    /// Fixed at sign-in; rotation never moves it.
    /// </summary>
    public required DateTime SessionExpires { get; init; }

    public DateTime? LastUsed { get; init; }

    /// <summary>
    /// Set by rotation as well as by sign-out.
    /// </summary>
    public DateTime? RevokedAt { get; init; }

    // Room for an IPv4-mapped IPv6 address with a zone index.
    [MaxLength(45)]
    public string? CreatedByIp { get; init; }

    [MaxLength(256)]
    public string? UserAgent { get; init; }

    public User? User { get; init; }
}

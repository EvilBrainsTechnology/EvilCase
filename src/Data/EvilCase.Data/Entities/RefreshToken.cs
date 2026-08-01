using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace EvilBrains.EvilCase.Data.Entities;

/// <summary>
/// One issued refresh token. Rotation writes a new row and revokes the old one, so a row is also the
/// audit trail of a session: the chain sharing a <see cref="AuthSessionId"/> is one browser.
/// </summary>
[Index(nameof(TokenHash), IsUnique = true)]
[Index(nameof(AuthSessionId))]
public record RefreshToken : IEntity
{
    [Key]
    public long Id { get; init; }

    public required long UserId { get; init; }

    /// <summary>
    /// Shared by every token of one rotation chain. Revoking a session, and revoking a chain after a
    /// replayed token, both work on this rather than on the individual rows.
    /// </summary>
    public required Guid AuthSessionId { get; init; }

    /// <summary>
    /// SHA-256 of the token, hex. The token is 256 bits of randomness, so a password KDF would only cost
    /// time on every refresh; what this protects against is a database dump handing out live sessions.
    /// </summary>
    [MaxLength(64)]
    public required string TokenHash { get; init; }

    public required DateTime Created { get; init; }

    /// <summary>
    /// When this token stops being accepted. Rotation moves it forward.
    /// </summary>
    public required DateTime Expires { get; init; }

    /// <summary>
    /// When the chain stops being accepted, however often it rotates. Fixed at sign-in.
    /// </summary>
    public required DateTime SessionExpires { get; init; }

    public DateTime? LastUsed { get; init; }

    /// <summary>
    /// Set by rotation as well as by sign-out. A token presented after this is a replay, and the whole
    /// chain goes with it.
    /// </summary>
    public DateTime? RevokedAt { get; init; }

    // Room for an IPv4-mapped IPv6 address with a zone index.
    [MaxLength(45)]
    public string? CreatedByIp { get; init; }

    [MaxLength(256)]
    public string? UserAgent { get; init; }

    public User? User { get; init; }
}

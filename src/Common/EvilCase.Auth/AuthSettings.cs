using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

internal sealed record AuthSettings
{
    [Required]
    [ValidateObjectMembers]
    public required JwtSettings Jwt { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required RefreshTokenSettings RefreshToken { get; init; }

    [Required]
    [ValidateObjectMembers]
    public required LockoutSettings Lockout { get; init; }

    /// <summary>
    /// Optional: an environment that names no seed credentials simply gets no seeded account.
    /// </summary>
    [ValidateObjectMembers]
    public SeedSettings? Seed { get; init; }

    internal sealed record JwtSettings
    {
        [Required]
        public required string Issuer { get; init; }

        [Required]
        public required string Audience { get; init; }

        /// <summary>
        /// Short on purpose: the token cannot be revoked, so its lifetime is the window a stolen one
        /// stays usable. The refresh token is what keeps the user signed in past it.
        /// </summary>
        [Required]
        public required TimeSpan AccessTokenExpiration { get; init; }

        // HS256 rejects a key shorter than 256 bits at signing time, which would be the first request.
        [Required]
        [MinLength(32)]
        public required string Key { get; init; }
    }

    internal sealed record RefreshTokenSettings
    {
        /// <summary>
        /// How long one issued refresh token stays valid. Every rotation starts it over.
        /// </summary>
        [Required]
        public required TimeSpan Expiration { get; init; }

        /// <summary>
        /// The ceiling on a rotation chain, fixed when the user signs in. However diligently a browser
        /// refreshes, the session ends here and the password is asked for again.
        /// </summary>
        [Required]
        public required TimeSpan SessionExpiration { get; init; }
    }

    internal sealed record LockoutSettings
    {
        [Range(1, 100)]
        public required int MaxFailedAttempts { get; init; }

        [Required]
        public required TimeSpan Duration { get; init; }
    }

    internal sealed record SeedSettings
    {
        [EmailAddress]
        [StringLength(256)]
        public string? Email { get; init; }

        [StringLength(128, MinimumLength = 12)]
        public string? Password { get; init; }
    }
}

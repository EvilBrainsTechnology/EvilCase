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

    /// <summary>
    /// Blank counts as unset, the way <c>UserSeeder</c> already reads it. An environment that names no
    /// seed still passes the key along — a compose file interpolating <c>${VAR:-}</c> hands over an empty
    /// string, not nothing at all — and the validation below would otherwise refuse to start over a
    /// deployment that simply seeds no account.
    /// </summary>
    internal sealed record SeedSettings
    {
        [EmailAddress]
        [StringLength(256)]
        public string? Email
        {
            get;
            init => field = Unset(value);
        }

        [StringLength(128, MinimumLength = 12)]
        public string? Password
        {
            get;
            init => field = Unset(value);
        }

        private static string? Unset(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}

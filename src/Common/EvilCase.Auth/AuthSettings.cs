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

    [ValidateObjectMembers]
    public SeedSettings? Seed { get; init; }

    internal sealed record JwtSettings
    {
        [Required]
        public required string Issuer { get; init; }

        [Required]
        public required string Audience { get; init; }

        [Required]
        public required TimeSpan AccessTokenExpiration { get; init; }

        // HS256 rejects a key shorter than 256 bits at signing time, which would be the first request.
        [Required]
        [MinLength(32)]
        public required string Key { get; init; }
    }

    internal sealed record RefreshTokenSettings
    {
        [Required]
        public required TimeSpan Expiration { get; init; }

        /// <summary>
        /// The ceiling on a whole rotation chain, fixed at sign-in.
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
    /// Blank counts as unset: compose's ${VAR:-} hands over an empty string the attributes would otherwise
    /// refuse at start.
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

        private static string? Unset(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value;
        }
    }
}

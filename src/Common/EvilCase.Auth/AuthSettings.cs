using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace EvilBrains.EvilCase.Auth;

internal sealed record AuthSettings
{
    [Required]
    [ValidateObjectMembers]
    public required JwtSettings Jwt { get; init; }

    internal sealed record JwtSettings
    {
        [Required]
        public required string Issuer { get; init; }

        [Required]
        public required string Audience { get; init; }

        [Required]
        public required TimeSpan TokenExpiration { get; init; }

        // HS256 rejects a key shorter than 256 bits at signing time, which would be the first request.
        [Required]
        [MinLength(32)]
        public required string Key { get; init; }
    }
}

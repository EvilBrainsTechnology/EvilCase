using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace EvilCase.Auth;

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

        [Required]
        public required string Key { get; init; }
    }
}

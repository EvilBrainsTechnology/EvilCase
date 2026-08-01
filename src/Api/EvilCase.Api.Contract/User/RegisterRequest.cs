using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.User;

public record RegisterRequest
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required]
    [StringLength(128, MinimumLength = 12)]
    public required string Password { get; init; }
}

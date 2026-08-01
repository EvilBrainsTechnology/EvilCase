using System.ComponentModel.DataAnnotations;

namespace EvilBrains.EvilCase.Api.Contract.User;

public record LoginRequest
{
    // Length caps only: the credentials of an existing account are whatever they were registered with,
    // and an unbounded password would be handed to PBKDF2 by an anonymous caller.
    [Required]
    [StringLength(256)]
    public required string Email { get; init; }

    [Required]
    [StringLength(128)]
    public required string Password { get; init; }
}

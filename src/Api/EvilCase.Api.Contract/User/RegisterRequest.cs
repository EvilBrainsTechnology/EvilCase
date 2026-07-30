namespace EvilBrains.EvilCase.Api.Contract.User;

public record RegisterRequest
{
    public required string Email { get; init; }

    public required string Password { get; init; }
}

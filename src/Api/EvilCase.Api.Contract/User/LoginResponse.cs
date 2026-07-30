namespace EvilBrains.EvilCase.Api.Contract.User;

public record LoginResponse
{
    public required string Email { get; init; }

    public required string Token { get; init; }
}

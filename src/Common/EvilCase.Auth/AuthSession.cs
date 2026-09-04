using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Auth;

public sealed record AuthSession
{
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpires { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime RefreshTokenExpires { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

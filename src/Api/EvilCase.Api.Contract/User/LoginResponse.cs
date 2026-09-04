using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// No refresh token here: it travels only in the HttpOnly cookie.
/// </summary>
public sealed record LoginResponse
{
    public required string AccessToken { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

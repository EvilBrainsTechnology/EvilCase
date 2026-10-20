using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// No tokens here: both travel only in HttpOnly cookies.
/// </summary>
public sealed record LoginResponse
{
    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

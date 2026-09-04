using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// In memory only, never in storage: a reload throws it away and the refresh cookie brings it back.
/// </summary>
internal sealed record AccessTokenState
{
    public required string Token { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// A signed-in user, as everything the caller has to hand back to the browser. The access token goes
/// into the response body, the refresh token into a cookie — this is the only place the raw refresh
/// token exists, as the store keeps nothing but its hash.
/// </summary>
public sealed record AuthSession
{
    public required string AccessToken { get; init; }

    public required DateTime AccessTokenExpires { get; init; }

    public required string RefreshToken { get; init; }

    public required DateTime RefreshTokenExpires { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

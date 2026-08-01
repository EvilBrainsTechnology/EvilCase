namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// What a sign-in or a refresh hands back. The refresh token is deliberately absent: it travels in a
/// cookie the browser's scripts cannot read, and only ever there.
/// </summary>
public record LoginResponse
{
    public required string AccessToken { get; init; }

    /// <summary>
    /// When the access token stops being accepted, so the client can renew ahead of a failed call
    /// rather than after one.
    /// </summary>
    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

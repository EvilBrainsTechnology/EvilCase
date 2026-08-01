using EvilBrains.EvilCase.Api.Contract.User;

namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// The signed-in user as the browser knows them. Lives in memory only: a reload throws it away and the
/// refresh cookie is what brings it back, so no script on the page can ever read a token out of storage.
/// </summary>
internal sealed record AccessTokenState
{
    public required string Token { get; init; }

    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

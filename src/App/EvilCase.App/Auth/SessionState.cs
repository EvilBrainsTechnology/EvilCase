using EvilBrains.EvilCase.Domain.Users;

namespace EvilBrains.EvilCase.App.Auth;

/// <summary>
/// Who is signed in and when the access cookie lapses. In memory only: a reload throws it away and the
/// refresh cookie brings it back. The tokens themselves are HttpOnly cookies the page never sees.
/// </summary>
internal sealed record SessionState
{
    public required DateTime ExpiresAt { get; init; }

    public required string Email { get; init; }

    public required UserRole Role { get; init; }
}

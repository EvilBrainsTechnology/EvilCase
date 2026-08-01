namespace EvilBrains.EvilCase.Auth;

internal sealed record AccessToken
{
    public required string Value { get; init; }

    public required DateTime ExpiresAt { get; init; }
}

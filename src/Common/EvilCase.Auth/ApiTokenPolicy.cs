namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Authenticates by an API token alone: neither the cookie nor a JWT satisfies it.
/// </summary>
public static class ApiTokenPolicy
{
    public const string Name = "ApiToken";
}

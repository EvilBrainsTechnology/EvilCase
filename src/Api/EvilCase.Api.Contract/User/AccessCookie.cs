namespace EvilBrains.EvilCase.Api.Contract.User;

public static class AccessCookie
{
    /// <summary>
    /// Carries the JWT access token; the <c>__Host-</c> prefix holds for the same reasons as on <see cref="RefreshCookie.Name"/>.
    /// </summary>
    public const string Name = "__Host-evilcase-access";
}

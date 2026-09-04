namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// Shared with the host's rate limiter; a mismatch leaves it guarding nothing.
/// </summary>
public static class AuthRoute
{
    /// <summary>
    /// Relative, as the client generator requires.
    /// </summary>
    public const string Template = "api/auth";

    public const string Path = "/" + Template;

    public const string LoginPath = Path + "/login";

    public const string RefreshPath = Path + "/refresh";

    public const string LogoutPath = Path + "/logout";
}

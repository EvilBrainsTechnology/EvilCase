namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// The route the authentication endpoints live under. The controller declares it and the host rate
/// limits it, so both have to name the same prefix: renaming it in one place only would leave the
/// limiter guarding a path no controller serves.
/// </summary>
public static class AuthRoute
{
    /// <summary>
    /// The controller route template. Relative, as the client generator requires.
    /// </summary>
    public const string Template = "api/auth";

    /// <summary>
    /// The same route as a request path, for <c>PathString</c> comparisons.
    /// </summary>
    public const string Path = "/" + Template;

    /// <summary>
    /// Sign-in. Limited harder than the rest: it is the only anonymous endpoint where guessing pays off.
    /// </summary>
    public const string LoginPath = Path + "/login";

    /// <summary>
    /// Token renewal. Limited far more loosely — every open tab of every signed-in user hits it.
    /// </summary>
    public const string RefreshPath = Path + "/refresh";

    /// <summary>
    /// Signing this one browser out. Anonymous like the two above, because an access token that has run
    /// out must not be what stops someone from leaving.
    /// </summary>
    public const string LogoutPath = Path + "/logout";
}

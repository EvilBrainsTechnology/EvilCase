namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// The claim names the access token carries. Inbound claim mapping is turned off, so these are also the
/// types on the principal — server and browser both have to name them, and neither may spell them twice.
/// </summary>
public static class AuthClaims
{
    /// <summary>
    /// The user's identifier.
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// The user's e-mail; the principal's name claim.
    /// </summary>
    public const string Email = "unique_name";

    /// <summary>
    /// The user's role; the principal's role claim.
    /// </summary>
    public const string Role = "role";

    /// <summary>
    /// The refresh token chain this access token was issued from. Named apart from the browser session
    /// the logging pipeline carries as <c>XSessionId</c> — a log event can hold both, and a bare
    /// <c>SessionId</c> next to it would say nothing about which one it is.
    /// </summary>
    public const string AuthSessionId = "sid";
}

namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// Inbound claim mapping is off, so these are the principal's claim types too.
/// </summary>
public static class AuthClaims
{
    public const string Subject = "sub";

    public const string Email = "unique_name";

    public const string Role = "role";

    /// <summary>
    /// The refresh chain, not the browser session logging carries as XSessionId.
    /// </summary>
    public const string AuthSessionId = "sid";

    public const string Tenant = "tenant";
}

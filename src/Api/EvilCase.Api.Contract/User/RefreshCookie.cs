namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// The cookie carrying the refresh token. Named here rather than in the controller because the host's
/// tests assert on it, and a second spelling would let the two drift apart unnoticed.
/// </summary>
public static class RefreshCookie
{
    /// <summary>
    /// The <c>__Host-</c> prefix is a promise the browser enforces: secure, path <c>/</c> and no domain,
    /// so no sibling subdomain can plant a cookie this host would then accept as its own.
    /// </summary>
    public const string Name = "__Host-evilcase-refresh";

    /// <summary>
    /// The prefix only holds for this exact path, so it is not a free choice.
    /// </summary>
    public const string Path = "/";
}

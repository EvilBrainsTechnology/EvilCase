namespace EvilBrains.EvilCase.Api.Contract.User;

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

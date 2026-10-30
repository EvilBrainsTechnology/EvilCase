namespace EvilBrains.EvilCase.Api.Contract.ApiTokens;

/// <summary>
/// Shared with the API token boundary, which keeps a token out of its own management.
/// </summary>
public static class ApiTokenRoute
{
    /// <summary>
    /// Relative, as the client generator requires.
    /// </summary>
    public const string Template = "api/api-tokens";

    public const string Path = "/" + Template;
}

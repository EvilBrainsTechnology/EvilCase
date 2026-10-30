namespace EvilBrains.EvilCase.Api.Contract.Files;

/// <summary>
/// The secret travels in the query string: the request path reaches every log event of the request, the query string
/// none.
/// </summary>
public static class FileLinkRoute
{
    public const string Template = "api/file-links";

    public const string Path = "/" + Template;

    public const string SecretParameter = "secret";

    public const string FileNameParameter = "fileName";
}

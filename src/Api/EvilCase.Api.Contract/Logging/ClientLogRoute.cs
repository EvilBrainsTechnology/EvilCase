namespace EvilBrains.EvilCase.Api.Contract.Logging;

/// <summary>
/// The route browser log batches are uploaded to. The controller declares it, the host silences its
/// successful requests and the browser sink silences its own side of them, so all three have to name the
/// same path: renaming it in one place only would make the upload log an event the next upload ships.
/// </summary>
public static class ClientLogRoute
{
    /// <summary>
    /// The controller route template. Relative, as the client generator requires.
    /// </summary>
    public const string Template = "api/logs";

    /// <summary>
    /// The same route as a request path, for <c>PathString</c> comparisons.
    /// </summary>
    public const string Path = "/" + Template;
}

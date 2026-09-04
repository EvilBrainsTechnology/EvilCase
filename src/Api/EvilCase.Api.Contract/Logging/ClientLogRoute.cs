namespace EvilBrains.EvilCase.Api.Contract.Logging;

/// <summary>
/// Named by the controller, the host's quiet paths and the browser sink; a mismatch makes the upload log an
/// event the next upload ships.
/// </summary>
public static class ClientLogRoute
{
    /// <summary>
    /// Relative, as the client generator requires.
    /// </summary>
    public const string Template = "api/logs";

    public const string Path = "/" + Template;
}

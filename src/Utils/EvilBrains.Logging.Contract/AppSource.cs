namespace EvilBrains.Logging.Contract;

/// <summary>
/// Which side of the application wrote the event.
/// </summary>
public static class AppSource
{
    public const string PropertyName = "AppSource";

    public const string Client = "Client";

    public const string Server = "Server";
}

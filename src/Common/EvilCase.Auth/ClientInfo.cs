namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// What the caller looked like when a session was opened. Recorded so a user can recognise their own
/// devices in the session list; never used to decide whether a token is accepted.
/// </summary>
public sealed record ClientInfo
{
    public static readonly ClientInfo Unknown = new();

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// One live rotation chain, as the user sees it in the list of their signed-in devices.
/// </summary>
public sealed record UserSession
{
    public required Guid SessionId { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime Expires { get; init; }

    public DateTime? LastUsed { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

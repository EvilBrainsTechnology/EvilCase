namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// One live rotation chain, as the user sees it in the list of their signed-in devices.
/// </summary>
public sealed record UserSession
{
    public required Guid AuthSessionId { get; init; }

    /// <summary>
    /// When the user signed in, not when the chain last rotated.
    /// </summary>
    public required DateTime Created { get; init; }

    public required DateTime Expires { get; init; }

    /// <summary>
    /// When this session last renewed, which for one that never has is the sign-in itself.
    /// </summary>
    public required DateTime LastUsed { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

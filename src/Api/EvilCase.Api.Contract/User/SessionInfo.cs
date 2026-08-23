namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// One place the user is signed in. Shown so they can recognise a device that is not theirs; the way
/// to act on it is to sign out everywhere.
/// </summary>
public sealed record SessionInfo
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

    /// <summary>
    /// Whether this is the session the request asking for the list was made from.
    /// </summary>
    public required bool IsCurrent { get; init; }
}

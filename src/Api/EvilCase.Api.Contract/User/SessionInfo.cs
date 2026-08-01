namespace EvilBrains.EvilCase.Api.Contract.User;

/// <summary>
/// One place the user is signed in. Shown so they can recognise a device that is not theirs; the way
/// to act on it is to sign out everywhere.
/// </summary>
public record SessionInfo
{
    public required Guid SessionId { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime Expires { get; init; }

    public DateTime? LastUsed { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    /// <summary>
    /// Whether this is the session the request asking for the list was made from.
    /// </summary>
    public required bool IsCurrent { get; init; }
}

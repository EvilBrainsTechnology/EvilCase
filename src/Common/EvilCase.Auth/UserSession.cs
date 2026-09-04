namespace EvilBrains.EvilCase.Auth;

public sealed record UserSession
{
    public required Guid AuthSessionId { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime Expires { get; init; }

    public required DateTime LastUsed { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}

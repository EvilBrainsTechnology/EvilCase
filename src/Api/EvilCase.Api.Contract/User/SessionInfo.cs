namespace EvilBrains.EvilCase.Api.Contract.User;

public sealed record SessionInfo
{
    public required Guid AuthSessionId { get; init; }

    public required DateTime Created { get; init; }

    public required DateTime Expires { get; init; }

    public required DateTime LastUsed { get; init; }

    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }

    public required bool IsCurrent { get; init; }
}

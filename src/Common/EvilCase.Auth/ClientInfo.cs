namespace EvilBrains.EvilCase.Auth;

/// <summary>
/// Shown in the session list only; never a factor in accepting a token.
/// </summary>
public sealed record ClientInfo
{
    public string? IpAddress { get; init; }

    public string? UserAgent { get; init; }
}
